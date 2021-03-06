﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Library.Extensions;

namespace VisualLocalizer.Library.Components {

    /// <summary>
    /// Represents list of namespaces used (declared or imported) in a file
    /// </summary>
    public class NamespacesList : List<UsedNamespaceItem> {
        /// <summary>
        /// GUID of the ASP .NET website projects
        /// </summary>
        private const string WebSiteProjectGuid = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";

        /// <summary>
        /// Default namespace for ASP .NET website projects
        /// </summary>
        private const string GlobalWebSiteResourcesNamespace = "Resources";
        
        /// <summary>
        /// Adds a new entry to this list
        /// </summary>
        /// <param name="namespaceName">Full name of the namespace</param>
        /// <param name="alias">Alias (can be null)</param>
        /// <param name="isImport">True if the namespace comes from the import statement</param>
        public void Add(string namespaceName, string alias, bool isImport) {
            if (string.IsNullOrEmpty(namespaceName)) throw new ArgumentNullException("namespaceName");

            Add(new UsedNamespaceItem(namespaceName, alias, isImport));
        }
        
        /// <summary>
        /// Returns true if given full namespace name exists in this list
        /// </summary>        
        public bool ContainsNamespace(string namespaceName) {
            if (string.IsNullOrEmpty(namespaceName)) throw new ArgumentNullException("namespaceName");

            foreach (var item in this)
                if (item.Namespace == namespaceName) return true;
            return false;
        }

        /// <summary>
        /// Gets alias (if defined) for given namespace
        /// </summary>        
        public string GetAlias(string namespaceName) {
            if (string.IsNullOrEmpty(namespaceName)) throw new ArgumentNullException("namespaceName");

            foreach (var item in this)
                if (item.Namespace == namespaceName) return item.Alias;
            return null;
        }

        /// <summary>
        /// Gets namespace declared with given alias
        /// </summary>        
        public string GetNamespace(string alias) {
            if (string.IsNullOrEmpty(alias)) throw new ArgumentNullException("alias");

            foreach (var item in this)
                if (item.Alias == alias) return item.Namespace;
            return null;
        }

        /// <summary>
        /// Determines the way of referencing a resource item. 
        /// </summary>
        /// <param name="designerNamespace">Namespace of the ResX designer file (if any)</param>
        /// <param name="designerClass">Class of the ResX designer file</param>
        /// <param name="newKey">Resource key</param>
        /// <param name="project">Current project</param>
        /// <param name="referenceText">Output - instance of ReferenceText info</param>
        /// <returns>True, of new import should be added to the document</returns>
        public bool ResolveNewElement(string designerNamespace, string designerClass, string newKey, Project project, out ReferenceString referenceText) {
            if (project == null) throw new ArgumentNullException("project");

            referenceText = new ReferenceString(designerClass, newKey);
            bool addUsing = true;
            bool matchedOnImported = false;

            if (designerNamespace == GlobalWebSiteResourcesNamespace && project.Kind.ToUpperInvariant() == WebSiteProjectGuid) {
                // website projects may lack codeModel - must use safe method
                referenceText.NamespacePart = GlobalWebSiteResourcesNamespace;
                return false;
            } else {
                List<Project> referencedProjects = project.GetReferencedProjects();
                referencedProjects.Insert(0, project);

                foreach (UsedNamespaceItem item in this) {
                    // try obtain the class
                    string fullName = item.Namespace + "." + designerClass;
                    CodeType codeType = null;
                    foreach (Project p in referencedProjects) {
                        try {
                            codeType = p.CodeModel.CodeTypeFromFullName(fullName);
                            if (codeType != null) break;
                        } catch {
                            codeType = null;
                        }
                    }

                    if (codeType != null) { // class with given name exists
                        if (addUsing) { // we haven't yet found a match
                            addUsing = false;
                            matchedOnImported = item.IsImported;
                            if (item.Namespace == designerNamespace) { // it's the right class
                                if (!string.IsNullOrEmpty(item.Alias)) referenceText.NamespacePart = item.Alias;
                            } else { // it's a wrong class and referencing it without prefix would cause error
                                string newAlias = GetAlias(designerNamespace);
                                if (!string.IsNullOrEmpty(newAlias)) { // add alias
                                    referenceText.NamespacePart = newAlias;
                                } else { // add full namespace
                                    referenceText.NamespacePart = designerNamespace;
                                }
                            }
                        } else if (item.IsImported && matchedOnImported) { // such class already exists - must use full name
                            addUsing = false;
                            referenceText.NamespacePart = designerNamespace;
                        }
                    }

                }
            }
            return addUsing;
        }

        /// <summary>
        /// Returns first namespace in which given class could belong (such combination exists)
        /// </summary>        
        public UsedNamespaceItem ResolveNewReference(string referenceClass, Project project) {
            UsedNamespaceItem result = null;
            foreach (UsedNamespaceItem item in this) {
                if (item.Namespace == GlobalWebSiteResourcesNamespace && project.Kind.ToUpperInvariant() == WebSiteProjectGuid) {
                    return item;
                } else {
                    string fullName = item.Namespace + "." + referenceClass;
                    CodeType codeType = null;
                    List<Project> referencedProjects = project.GetReferencedProjects();
                    referencedProjects.Insert(0, project);

                    foreach (Project p in referencedProjects) {
                        try {
                            codeType = p.CodeModel.CodeTypeFromFullName(fullName);
                            if (codeType != null) break;
                        } catch {
                            codeType = null;
                        }
                    }

                    if (codeType != null) {
                        result = item;
                        break;
                    }

                }
            }

            return result;
        }

    }

    /// <summary>
    /// Represents one "used" (imported or declared) namespace
    /// </summary>
    public class UsedNamespaceItem {
        /// <summary>
        /// Initializes a new instance of the <see cref="UsedNamespaceItem"/> class.
        /// </summary>
        /// <param name="namespaceName">Qualified name of the namespace</param>
        /// <param name="alias">Alias of the namespace (can be null)</param>
        /// <param name="isImported">True if the namespace comes from an import statement</param>
        public UsedNamespaceItem(string namespaceName, string alias, bool isImported) {
            this.Namespace = namespaceName;
            this.Alias = alias;
            this.IsImported = isImported;
        }

        /// <summary>
        /// Full namespace name
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Alias - can be null
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// True if the namespace comes from the import statement
        /// </summary>
        public bool IsImported { get; set; }
    }

    /// <summary>
    /// Represents reference to a resource - consists of a namespace, a class and a key
    /// </summary>
    public class ReferenceString {

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceString"/> class.
        /// </summary>
        public ReferenceString() : this(null, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceString"/> class.
        /// </summary>
        /// <param name="classPart">The name of the resource class</param>
        /// <param name="keyPart">The name of the resource key</param>
        public ReferenceString(string classPart, string keyPart) : this(null, classPart, keyPart) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceString"/> class.
        /// </summary>
        /// <param name="namespacePart">The namespace in the reference (can be null)</param>
        /// <param name="classPart">The name of the resource class</param>
        /// <param name="keyPart">The name of the resource key</param>
        public ReferenceString(string namespacePart, string classPart, string keyPart) {
            this.NamespacePart = namespacePart;
            this.ClassPart = classPart;
            this.KeyPart = keyPart;
        }

        /// <summary>
        /// Namespace part of the reference (can be null)
        /// </summary>
        public string NamespacePart { get; set; }

        /// <summary>
        /// Class name
        /// </summary>
        public string ClassPart { get; set; }

        /// <summary>
        /// Resource key name
        /// </summary>
        public string KeyPart { get; set; }

    }
}
