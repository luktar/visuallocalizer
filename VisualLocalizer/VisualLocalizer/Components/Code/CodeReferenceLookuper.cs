﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;
using VisualLocalizer.Components.AspxParser;

namespace VisualLocalizer.Components {
    internal class CodeReferenceLookuper<T> : AbstractCodeLookuper where T:CodeReferenceResultItem, new() {

        public CodeReferenceLookuper(string text, TextPoint startPoint, Trie<CodeReferenceTrieElement> Trie, NamespacesList usedNamespaces, bool isWithinLocFalse, Project project)
        : this(text, startPoint.LineCharOffset - 1, startPoint.Line, startPoint.AbsoluteCharOffset + startPoint.Line - 2, Trie, usedNamespaces, isWithinLocFalse, project) 
        {}

        public CodeReferenceLookuper(string text, BlockSpan blockSpan, Trie<CodeReferenceTrieElement> Trie, NamespacesList usedNamespaces, Project project)
            : this(text, blockSpan.StartIndex-1, blockSpan.StartLine, blockSpan.AbsoluteCharOffset, Trie, usedNamespaces, false, project) { }

        protected CodeReferenceLookuper(string text, int currentIndex, int currentLine, int currentOffset, 
            Trie<CodeReferenceTrieElement> Trie, NamespacesList usedNamespaces, bool isWithinLocFalse, Project project) {
            this.text = text;
            this.CurrentIndex = currentIndex;
            this.CurrentLine = currentLine;
            this.CurrentAbsoluteOffset = currentOffset;
            this.Trie = Trie;
            this.UsedNamespaces = usedNamespaces;
            this.IsWithinLocFalse = isWithinLocFalse;
            this.Project = project;
        }

        protected Project Project { get; set; }
        protected NamespacesList UsedNamespaces { get; set; }
        protected Trie<CodeReferenceTrieElement> Trie { get; set; }
        protected int ReferenceStartLine { get; set; }
        protected int ReferenceStartIndex { get; set; }
        protected int ReferenceStartOffset { get; set; }
        protected int AbsoluteReferenceLength { get; set; }
        
        public List<T> LookForReferences() {
            bool insideComment = false, insideString = false, isVerbatimString = false;
            bool skipLine = false;
            currentChar = '?';
            previousChar = '?';
            previousPreviousChar = '?';
            previousPreviousPreviousChar = '?';
            stringStartChar = '?';
            List<T> list = new List<T>();
            CodeReferenceTrieElement currentElement = Trie.Root; 
            StringBuilder prefixBuilder = new StringBuilder();
            string prefix = null;
            char lastNonWhitespaceChar = '?';

            for (int i = 0; i < text.Length; i++) {
                previousPreviousPreviousChar = previousPreviousChar;
                previousPreviousChar = previousChar;
                previousChar = currentChar;
                currentChar = text[i];

                if (skipLine) {
                    if (currentChar == '\n') {
                        previousChar = '?';
                        previousPreviousChar = '?';
                        skipLine = false;
                    }
                } else {
                    PreProcessChar(ref insideComment, ref insideString, ref isVerbatimString, out skipLine);

                    if (currentChar.CanBePartOfIdentifier() && !lastNonWhitespaceChar.CanBePartOfIdentifier() && lastNonWhitespaceChar != '.') {
                        ReferenceStartIndex = CurrentIndex;
                        ReferenceStartLine = CurrentLine;
                        ReferenceStartOffset = CurrentAbsoluteOffset;
                        AbsoluteReferenceLength = 1;
                        prefixBuilder.Length = 0;
                        prefixBuilder.Append(currentChar);
                        prefix = null;
                    } else {
                        AbsoluteReferenceLength++;
                        if (!char.IsWhiteSpace(currentChar)) prefixBuilder.Append(currentChar);
                    }

                    if (!insideString && !insideComment) {
                        if (!char.IsWhiteSpace(currentChar)) {
                            bool wasAtRoot = currentElement == Trie.Root;
                            currentElement = Trie.Step(currentElement, currentChar);

                            if (wasAtRoot && currentElement != Trie.Root) {
                                if (previousChar.CanBePartOfIdentifier()) {
                                    currentElement = Trie.Root;
                                } else if (prefixBuilder.Length >= 2) {
                                    if (prefixBuilder[prefixBuilder.Length - 2] == '.') {
                                        prefix = prefixBuilder.ToString(0, prefixBuilder.Length - 2);
                                    } else {
                                        prefix = null;
                                        ReferenceStartIndex = CurrentIndex;
                                        ReferenceStartLine = CurrentLine;
                                        ReferenceStartOffset = CurrentAbsoluteOffset;
                                        AbsoluteReferenceLength = 1;
                                    }
                                }
                            }
                            
                            if (currentElement.IsTerminal && (i == text.Length - 1 || !text[i + 1].CanBePartOfIdentifier())) {
                                AddResult(list, currentElement.Word, prefix, currentElement.Infos);
                            }                                                           
                        }
                    } else {
                        currentElement = Trie.Root;
                    }
                    if (!insideString  || insideComment) {
                        isVerbatimString = false;
                    }
                }

                if (!char.IsWhiteSpace(currentChar)) lastNonWhitespaceChar = currentChar;
                Move();
            }

            return list;
        }

        private CodeReferenceInfo GetInfoWithNamespace(List<CodeReferenceInfo> list, string nmspc) {
            CodeReferenceInfo nfo = null;
            foreach (var item in list)
                if (item.Origin.Namespace == nmspc) {
                    nfo = item;
                    break;
                }
            return nfo;
        }

        protected virtual T AddResult(List<T> list, string referenceText,string prefix, List<CodeReferenceInfo> trieElementInfos) {            
            CodeReferenceInfo info = null;
            string[] t = referenceText.Split('.');
            if (t.Length != 2) throw new Exception("Code parse error.");
            string referenceClass = t[0];

            if (string.IsNullOrEmpty(prefix)) {                
                UsedNamespaceItem item = UsedNamespaces.ResolveNewReference(referenceClass, Project);

                if (item != null) {
                    info = GetInfoWithNamespace(trieElementInfos, item.Namespace);
                    if (info == null) return null;
                }
            } else {
                string aliasNamespace = UsedNamespaces.GetNamespace(prefix);
                if (!string.IsNullOrEmpty(aliasNamespace)) {
                    info = GetInfoWithNamespace(trieElementInfos, aliasNamespace);
                    if (info == null) return null;                    
                } else {
                    foreach (var i in trieElementInfos)
                        if (i.Origin.Namespace == prefix) {
                            info = i;
                            break;
                        }

                    if (info == null) {
                        UsedNamespaceItem item = UsedNamespaces.ResolveNewReference(prefix + "." + referenceClass, Project);
                        if (item == null) return null;

                        info = GetInfoWithNamespace(trieElementInfos, item.Namespace + "." + prefix);
                        if (info == null) return null;                                 
                    }
                }                
            }
            
            if (info != null) {
                TextSpan span = new TextSpan();
                span.iStartLine = ReferenceStartLine - 1;
                span.iStartIndex = ReferenceStartIndex;
                span.iEndLine = CurrentLine - 1;
                span.iEndIndex = CurrentIndex + 1;

                var resultItem = new T();
                resultItem.Value = info.Value;
                resultItem.SourceItem = this.SourceItem;
                resultItem.ReplaceSpan = span;
                resultItem.AbsoluteCharOffset = ReferenceStartOffset;
                resultItem.AbsoluteCharLength = AbsoluteReferenceLength;
                resultItem.DestinationItem = info.Origin;                
                resultItem.FullReferenceText = string.Format("{0}.{1}.{2}", info.Origin.Namespace, info.Origin.Class, referenceText.Substring(referenceText.LastIndexOf('.') + 1));
                resultItem.IsWithinLocalizableFalse = IsWithinLocFalse;
                resultItem.Key = info.Key;

                if (string.IsNullOrEmpty(prefix)) {
                    resultItem.OriginalReferenceText = referenceText;
                } else {
                    resultItem.OriginalReferenceText = string.Format("{0}.{1}", prefix, referenceText);
                }

                list.Add(resultItem);

                return resultItem;
            } else throw new Exception("Cannot determine reference target.");
        }
        
    }
}
