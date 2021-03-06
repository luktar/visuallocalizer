﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Commands;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Commands.Inline;

namespace VLUnitTests.VLTests {

    /// <summary>
    /// Tests for ad-hoc version of the "inline" command.
    /// </summary>
    [TestClass()]
    public class InlineTest {

        /// <summary>
        /// Tests ASP .NET (C# variant) files
        /// </summary>
        [TestMethod()]
        [DeploymentItem("VisualLocalizer.dll")]
        public void AspNetInlineTest1() {
            Agent.EnsureSolutionOpen();

            AspNetInlineCommand_Accessor target = new AspNetInlineCommand_Accessor();
            Window window = Agent.GetDTE().OpenFile(null, Agent.AspNetReferencesTestFile1);

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.AspNetReferencesTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.AspNetReferencesTestFile1, false);
            var expected = AspNetBatchInlineTests.GetExpectedResultsFor(Agent.AspNetReferencesTestFile1);

            RunTest(target, view, lines, expected);

            window.Detach();
            window.Close(vsSaveChanges.vsSaveChangesNo);
        }

        /// <summary>
        /// Tests ASP .NET (VB variant) files
        /// </summary>
        [TestMethod()]
        [DeploymentItem("VisualLocalizer.dll")]
        public void AspNetInlineTest2() {
            Agent.EnsureSolutionOpen();

            AspNetInlineCommand_Accessor target = new AspNetInlineCommand_Accessor();
            Window window = Agent.GetDTE().OpenFile(null, Agent.AspNetReferencesTestFile2);

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.AspNetReferencesTestFile2, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.AspNetReferencesTestFile2, false);
            var expected = AspNetBatchInlineTests.GetExpectedResultsFor(Agent.AspNetReferencesTestFile2);

            RunTest(target, view, lines, expected);

            window.Detach();
            window.Close(vsSaveChanges.vsSaveChangesNo);
        }

        /// <summary>
        /// Tests C# files
        /// </summary>
        [TestMethod()]
        [DeploymentItem("VisualLocalizer.dll")]
        public void CSharpInlineTest() {
            Agent.EnsureSolutionOpen();

            CSharpInlineCommand_Accessor target = new CSharpInlineCommand_Accessor();
            Window window = Agent.GetDTE().OpenFile(null, Agent.CSharpReferencesTestFile1);

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.CSharpReferencesTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.CSharpReferencesTestFile1, false);
            var expected = CSharpBatchInlineTest.GetExpectedResultsFor(Agent.CSharpReferencesTestFile1);

            RunTest(target, view, lines, expected);

            window.Detach();
            window.Close(vsSaveChanges.vsSaveChangesNo);
        }

        /// <summary>
        /// Tests VB files
        /// </summary>
        [TestMethod()]
        [DeploymentItem("VisualLocalizer.dll")]
        public void VBInlineTest() {
            Agent.EnsureSolutionOpen();

            VBInlineCommand_Accessor target = new VBInlineCommand_Accessor();
            Window window = Agent.GetDTE().OpenFile(null, Agent.VBReferencesTestFile1);

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.VBReferencesTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.VBReferencesTestFile1, false);
            var expected = VBBatchInlineTest.GetExpectedResultsFor(Agent.VBReferencesTestFile1);

            RunTest(target, view, lines, expected);

            window.Detach();
            window.Close(vsSaveChanges.vsSaveChangesNo);
        }

        /// <summary>
        /// Generic test for the ad-hoc inline commands.
        /// </summary>
        /// <typeparam name="T">Expected type of result items</typeparam>
        /// <param name="target">Command to execute</param>
        /// <param name="view"></param>
        /// <param name="lines"></param>
        /// <param name="expectedList">List of expected result items</param>
        protected void RunTest<T>(InlineCommand_Accessor<T> target, IVsTextView view, IVsTextLines lines, List<AbstractResultItem> expectedList) where T : AbstractResultItem, new() {
            Random rnd = new Random();
            target.InitializeVariables();

            // simulate right-click around each of expected result items and verify that move command reacts
            foreach (AbstractResultItem expectedItem in expectedList) {
                Assert.IsTrue(expectedItem.ReplaceSpan.iStartLine >= 0);
                Assert.IsTrue(expectedItem.ReplaceSpan.iEndLine >= 0);

                // each result item will be clicked at every its characted
                for (int line = expectedItem.ReplaceSpan.iStartLine; line <= expectedItem.ReplaceSpan.iEndLine; line++) {
                    int begin;
                    int end;

                    if (line == expectedItem.ReplaceSpan.iStartLine) {
                        begin = expectedItem.ReplaceSpan.iStartIndex;
                    } else {
                        begin = 0;
                    }

                    if (line == expectedItem.ReplaceSpan.iEndLine) {
                        end = expectedItem.ReplaceSpan.iEndIndex;
                    } else {
                        lines.GetLengthOfLine(line, out end);
                    }

                    for (int column = begin; column <= end; column++) {
                        // perform right-click
                        view.SetSelection(line, column, line, column);

                        // run the command
                        var actualItem = target.GetCodeReferenceResultItem();

                        Assert.IsNotNull(actualItem, "Actual item cannot be null");
                        actualItem.IsWithinLocalizableFalse = expectedItem.IsWithinLocalizableFalse; // can be ignored

                        BatchTestsBase.ValidateItems(expectedItem, actualItem);
                    }

                    // create random selections within the result item
                    for (int i = 0; i < 5; i++) {
                        int b = rnd.Next(begin, end + 1);
                        int e = rnd.Next(b, end + 1);
                        view.SetSelection(line, b, line, e);
                        var actualItem = target.GetCodeReferenceResultItem();

                        actualItem.IsWithinLocalizableFalse = expectedItem.IsWithinLocalizableFalse; // can be ignored

                        BatchTestsBase.ValidateItems(expectedItem, actualItem);
                    }
                }

                if (expectedItem.ReplaceSpan.iStartIndex - 1 >= 0) {
                    view.SetSelection(expectedItem.ReplaceSpan.iStartLine, expectedItem.ReplaceSpan.iStartIndex - 1, expectedItem.ReplaceSpan.iStartLine, expectedItem.ReplaceSpan.iStartIndex - 1);
                    Assert.IsNull(target.GetCodeReferenceResultItem(), "For item " + expectedItem.Value);
                }

                int lineLength;
                lines.GetLengthOfLine(expectedItem.ReplaceSpan.iEndLine, out lineLength);

                if (expectedItem.ReplaceSpan.iEndIndex + 1 <= lineLength) {
                    view.SetSelection(expectedItem.ReplaceSpan.iEndLine, expectedItem.ReplaceSpan.iEndIndex + 1, expectedItem.ReplaceSpan.iEndLine, expectedItem.ReplaceSpan.iEndIndex + 1);
                    Assert.IsNull(target.GetCodeReferenceResultItem(), "For item " + expectedItem.Value);
                }
            }
        }

    }
}
