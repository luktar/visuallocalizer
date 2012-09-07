﻿using System;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.OLE.Interop;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using VisualLocalizer.Library;

namespace VisualLocalizer.Commands {
    internal abstract class AbstractCommand {

        protected VisualLocalizerPackage package;        
        protected IVsTextManager textManager;
        
        protected IVsTextLines textLines;
        protected IVsTextView textView;
        protected IOleUndoManager undoManager;        
        protected Document currentDocument;
        protected FileCodeModel2 currentCodeModel;

        public AbstractCommand(VisualLocalizerPackage package) {
            this.package = package;            
        }

        public virtual void Process() {
            currentDocument = package.DTE.ActiveDocument;
            if (currentDocument == null)
                throw new Exception("No selected document");
            currentCodeModel = currentDocument.ProjectItem.FileCodeModel as FileCodeModel2;
            if (currentCodeModel == null)
                throw new Exception("Current document has no CodeModel.");

            textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));

            int hr = textManager.GetActiveView(1, null, out textView);
            Marshal.ThrowExceptionForHR(hr);
            
            hr = textView.GetBuffer(out textLines);
            Marshal.ThrowExceptionForHR(hr);

            hr = textLines.GetUndoManager(out undoManager);
            Marshal.ThrowExceptionForHR(hr);           
        }

        protected bool IsWithinNamespace(TextPoint point, string newNamespace, out string alias) {
            alias = null;            

            CodeElement selectionNamespace = currentCodeModel.CodeElementFromPoint(point, vsCMElement.vsCMElementNamespace);
            string currentNamespace = selectionNamespace.FullName;

            if (currentNamespace == newNamespace) return true;

            bool alreadyUsing = false;
            foreach (CodeElement t in currentCodeModel.CodeElements)
                if (t.Kind == vsCMElement.vsCMElementImportStmt) {
                    string usingAlias, usingNmsName;
                    ParseUsing(t.StartPoint, t.EndPoint, out usingNmsName, out usingAlias);
                    if (usingNmsName == newNamespace) {
                        alreadyUsing = true;
                        alias = usingAlias;
                        break;
                    }
                }
            return alreadyUsing;
        }

        protected void ParseUsing(TextPoint start, TextPoint end, out string namespc, out string alias) {
            alias = null;

            string text;
            int hr = textLines.GetLineText(start.Line - 1, start.DisplayColumn - 1, end.Line - 1, end.DisplayColumn - 1, out text);
            Marshal.ThrowExceptionForHR(hr);

            text = text.Trim();
            if (!text.StartsWith(StringConstants.UsingStatement) || !text.EndsWith(";"))
                throw new Exception("Error while parsing using statement: " + text);

            text = text.Substring(StringConstants.UsingStatement.Length, text.LastIndexOf(';') - StringConstants.UsingStatement.Length);
            text = text.RemoveWhitespace();
            int eqIndex = text.IndexOf('=');
            if (eqIndex > 0) {
                alias = text.Substring(0, eqIndex);
                namespc = text.Substring(eqIndex + 1);
            } else {
                namespc = text;
            }
        }

        protected string GetTextOfSpan(TextSpan span) {
            string str;

            int hr = textLines.GetLineText(span.iStartLine, span.iStartIndex,
                span.iEndLine, span.iEndIndex, out str);
            Marshal.ThrowExceptionForHR(hr);

            return str;
        }

        protected TextSpan TrimSpan(TextSpan span, string textLine) {
            while (span.iEndIndex > 0 && char.IsWhiteSpace(textLine, span.iEndIndex - 1))
                span.iEndIndex--;

            while (span.iStartIndex < textLine.Length && char.IsWhiteSpace(textLine, span.iStartIndex))
                span.iStartIndex++;

            return span;
        }       

        protected int countAposRight(string text, int beginIndex, out int firstIndex) {
            int count = 0;
            firstIndex = -1;

            int p = beginIndex;
            if (p >= text.Length) return 0;

            char prevChar = (p - 1 >= 0 ? text[p - 1] : '?');
            char currentChar = text[p];

            while (p < text.Length) {
                if (currentChar == '"' && prevChar != '\\') {
                    if (count == 0) {
                        firstIndex = p;
                    }
                    count++;
                }

                p++;
                if (p >= text.Length) break;

                prevChar = currentChar;
                currentChar = text[p];
            }

            return count;
        }

        protected int countAposLeft(string text, int beginIndex, out int firstIndex) {
            int count = 0;
            firstIndex = -1;

            int p = beginIndex - 1;
            if (p <= 0) return 0;

            char prevChar = text[p];
            char currentChar = (p - 1 >= 0 ? text[p - 1] : '?');
            p--;

            while (p >= 0) {
                if (currentChar != '\\' && prevChar == '\"') {
                    if (count == 0) {
                        if (currentChar == '@')
                            firstIndex = p;
                        else
                            firstIndex = p + 1;
                    }
                    count++;
                }

                p--;
                prevChar = currentChar;

                if (p >= 0)
                    currentChar = text[p];
                else if (p == -1)
                    currentChar = '?';
                else break;
            }

            return count;
        }

        protected bool IsTextSpanInAttribute(TextSpan span) {
            bool ret = true;
            try {
                object point;
                
                textLines.CreateTextPoint(span.iStartLine, span.iStartIndex, out point);                
                currentCodeModel.CodeElementFromPoint(point as TextPoint, vsCMElement.vsCMElementAttribute);
            } catch (Exception) {
                ret = false;
            }
            return ret;
        }        
    }
}
