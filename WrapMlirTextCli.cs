using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using static TextWrapper.TextWrapper;

namespace WrapMlirText
{
    public partial class WrapMlirText
    {
        #if false
        private void buttonWrap_Click(object sender, EventArgs e)
        {
            string inputText = textBoxInput.Text;
            uint maximumLineLength = MaximumLineLength;
            uint lineIndentationPerLevel = LineIndentationPerLevel;

            var breakpointOpportunities = AssignLineBreakpointOpportunities(inputText, breakPairTable, categoryBreakFlags);
            var lineRanges = GetLineRanges(inputText, breakpointOpportunities, maximumLineLength, lineIndentationPerLevel);

            string tokensText = GetTokensText(inputText);
            textBoxTokens.Text = tokensText;
            textBoxTokens.SelectionStart = 0; // For some reason, setting the text also selects all the text. So clear it.
            textBoxTokens.SelectionLength = 0;
            string breakFlagsText = GetBreakFlagsText(inputText, breakpointOpportunities);
            textBoxBreakFlags.Text = breakFlagsText;
            textBoxBreakFlags.SelectionStart = 0;
            textBoxBreakFlags.SelectionLength = 0;
            string lineRangesText = GetLineRangesText(inputText, breakpointOpportunities, lineRanges, LineIndentationPerLevel);
            textBoxLineRanges.Text = lineRangesText;
            textBoxLineRanges.SelectionStart = 0;
            textBoxLineRanges.SelectionLength = 0;
            string wrappedText = GetWrappedText(inputText, breakpointOpportunities, lineRanges, LineIndentationPerLevel);
            textBoxOutput.Text = wrappedText;
            textBoxOutput.SelectionStart = 0;
            textBoxOutput.SelectionLength = 0;
        }
        #endif

        public static string GetTokensText(string inputText)
        {
            var tokensText = new StringBuilder();

            for (uint textPosition = 0; textPosition < inputText.Length;)
            {
                uint previousTextPosition = textPosition;
                TokenCategory category = ReadNextTokenCategory(inputText, ref textPosition);
                tokensText.Append(category.ToString());
                tokensText.Append(":\t\"");
                if (category != TokenCategory.LineBreak)
                {
                    tokensText.Append(inputText.Substring((int)previousTextPosition, (int)(textPosition - previousTextPosition)));
                }
                tokensText.Append("\"\r\n");
            }
            return tokensText.ToString();
        }

        public static string GetBreakFlagsText(string inputText, TextWrapper.TextWrapper.LineBreakpointOpportunity[] breakpointOpportunities)
        {
            var breakFlagsText = new StringBuilder();

            for (uint textPosition = 0; textPosition < inputText.Length; ++textPosition)
            {
                breakFlagsText.Append($"[{textPosition}]\t'{inputText[(int)textPosition]}'\tL{breakpointOpportunities[(int)textPosition].indentationLevel}\t{breakpointOpportunities[(int)textPosition].breakFlags}\r\n");
            }
            return breakFlagsText.ToString();
        }

        public static string GetLineRangesText(
            string inputText,
            TextWrapper.TextWrapper.LineBreakpointOpportunity[] breakpointOpportunities,
            List<LineRange> lineRanges,
            uint lineIndentationPerLevel
            )
        {
            var lineRangesText = new StringBuilder();

            foreach (var lineRange in lineRanges)
            {
                string lineText = inputText.Substring((int)lineRange.start, (int)lineRange.Length);
                if (lineText.EndsWith("\r\n"))
                {
                    lineText = lineText.Remove((int)lineText.Length - 2);
                }
                var indentationLevel = (lineRange.start < breakpointOpportunities.Length) ? breakpointOpportunities[(int)lineRange.start].indentationLevel : 0;
                lineRangesText.Append($"@{lineRange.start}..{lineRange.end} x{lineRange.Length} L{indentationLevel}\t\"{lineText}\"\r\n");
            }
            return lineRangesText.ToString();
        }
    }
}
