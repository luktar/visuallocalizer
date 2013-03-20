﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {
    internal class CSharpStringLookuper : CSharpLookuper<CSharpStringResultItem> {

        private CodeNamespace namespaceElement;
        private string methodElement;
        private string variableElement;
        private string ClassOrStructElement { get; set; }

        private static CSharpStringLookuper instance;

        private CSharpStringLookuper() { }

        public static CSharpStringLookuper Instance {
            get {
                if (instance == null) instance = new CSharpStringLookuper();
                return instance;
            }
        }

        public List<CSharpStringResultItem> LookForStrings(ProjectItem projectItem, bool isGenerated, string text, TextPoint startPoint, CodeNamespace namespaceElement,
            string classOrStructElement, string methodElement, string variableElement, bool isWithinLocFalse) {
            this.namespaceElement = namespaceElement;
            this.ClassOrStructElement = classOrStructElement;
            this.methodElement = methodElement;
            this.variableElement = variableElement;

            return LookForStrings(projectItem, isGenerated, text, startPoint, isWithinLocFalse);
        }

        protected override CSharpStringResultItem AddStringResult(List<CSharpStringResultItem> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            if (originalValue.StartsWith("@")) originalValue = originalValue.Substring(1);
            CSharpStringResultItem resultItem = base.AddStringResult(list, originalValue, isVerbatimString, isUnlocalizableCommented);
            
            resultItem.MethodElementName = methodElement;
            resultItem.NamespaceElement = namespaceElement;
            resultItem.VariableElementName = variableElement;
            resultItem.ClassOrStructElementName = ClassOrStructElement;
            resultItem.WasVerbatim = isVerbatimString;
            resultItem.Value=resultItem.Value.ConvertCSharpEscapeSequences(isVerbatimString);

            return resultItem;
        }
    }
}