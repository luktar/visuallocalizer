﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Commands;
using EnvDTE80;
using EnvDTE;
using VisualLocalizer.Library;
using VisualLocalizer.Library.Extensions;

namespace VisualLocalizer.Components.Code {

    /// <summary>
    /// Provides functionality for exploring VB code file using FileCodeModel. Content of each method and variable initializer
    /// is handled by some instance of AbstractBatchCommand, whose method LookupInCSharp() is called as callback.
    /// </summary>
    internal sealed class VBCodeExplorer : CSharpCodeExplorer {
        private VBCodeExplorer() { }

        private static VBCodeExplorer instance;
        public static new VBCodeExplorer Instance {
            get {
                if (instance == null) instance = new VBCodeExplorer();
                return instance;
            }
        }

        /// <summary>
        /// Explores given method using VB lookuper
        /// </summary> 
        protected override void Explore(AbstractBatchCommand parentCommand, CodeFunction2 codeFunction, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, Predicate<CodeElement> exploreable, bool isLocalizableFalse) {
            if (codeFunction.MustImplement) return; // method must not be abstract
            if (!exploreable(codeFunction as CodeElement)) return; // predicate must eval to true

            // there is no way of knowing whether a function is not declared 'extern'. In that case, following will throw an exception.
            string functionText = null;
            try {
                functionText = codeFunction.GetText(); // get method text
            } catch (Exception) { }
            if (string.IsNullOrEmpty(functionText)) return;

            TextPoint startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartBody);

            // is method decorated with Localizable(false)
            bool functionLocalizableFalse = (codeFunction as CodeElement).HasLocalizableFalseAttribute();

            var list = parentCommand.LookupInVB(functionText, startPoint, parentNamespace, codeClassOrStruct, codeFunction.Name, null, isLocalizableFalse || functionLocalizableFalse);

            // add context to result items (surounding few lines of code)
            EditPoint2 editPoint = (EditPoint2)startPoint.CreateEditPoint();
            foreach (AbstractResultItem item in list) {
                item.CodeModelSource = codeFunction;
                AddContextToItem(item, editPoint);
            }

            // read optional arguments initializers (just to show them - they cannot be moved)
            TextPoint headerStartPoint = null;
            try {
                headerStartPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartHeader);
            } catch (Exception) { }
            if (headerStartPoint == null) return;
            
            string headerText = codeFunction.GetHeaderText();

            // search method header
            var headerList = parentCommand.LookupInVB(headerText, headerStartPoint, parentNamespace, codeClassOrStruct, codeFunction.Name, null, isLocalizableFalse || functionLocalizableFalse);

            // add to list
            editPoint = (EditPoint2)startPoint.CreateEditPoint();
            foreach (AbstractResultItem item in headerList) {
                item.IsConst = true;
                item.CodeModelSource = codeFunction;
                AddContextToItem(item, editPoint);
            }
        }

        /// <summary>
        /// Explores given variable initializer using VB lookuper
        /// </summary>   
        protected override void Explore(AbstractBatchCommand parentCommand, CodeVariable2 codeVariable, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, Predicate<CodeElement> exploreable, bool isLocalizableFalse) {            
            if (codeVariable.InitExpression == null) return; // variable must have an initializer
            if (codeClassOrStruct.Kind == vsCMElement.vsCMElementStruct && !codeVariable.IsShared) return; // instance variable of structs cannot have initializers
            if (!exploreable(codeVariable as CodeElement)) return; // predicate must evaluate to true

            string initExpression = codeVariable.GetText(); // get text of initializer
            if (string.IsNullOrEmpty(initExpression)) return;

            TextPoint startPoint = codeVariable.StartPoint;

            // is variable decorated with Localizable(false)
            bool variableLocalizableFalse = (codeVariable as CodeElement).HasLocalizableFalseAttribute();

            // run lookuper using parent command
            var list = parentCommand.LookupInVB(initExpression, startPoint, parentNamespace, codeClassOrStruct, null, codeVariable.Name, isLocalizableFalse || variableLocalizableFalse);

            // add context to result items (surounding few lines of code)
            EditPoint2 editPoint = (EditPoint2)startPoint.CreateEditPoint();
            foreach (AbstractResultItem item in list) {
                item.IsConst = codeVariable.ConstKind == vsCMConstKind.vsCMConstKindConst;
                item.CodeModelSource = codeVariable;
                AddContextToItem(item, editPoint);
            }
        }
    }
}
