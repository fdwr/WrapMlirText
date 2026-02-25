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
    public partial class formMain : Form
    {
        // P/Invoke constants for Win32 messages
        private const int WM_UPDATEUISTATE = 0x0128;
        private const int UISF_HIDEACCEL = 0x2;
        private const int UIS_CLEAR = 0x2;
        private const int EM_SETTABSTOPS = 0x00CB;

        // UI configuration constants
        private const int TokensTabWidth = 20;
        private const int LineRangesTabWidth = 20;
        private const uint DefaultMaximumLineLength = 120;
        private const uint DefaultLineIndentationPerLevel = 4;

        public struct LineRangeAndTokenCategory
        {
            public LineRange lineRange;
            public TokenCategory tokenCategory;
        }

        private List<LineRangeAndTokenCategory> tokenRanges = new List<LineRangeAndTokenCategory>();
        private List<LineRange> lineRanges = new List<LineRange>();
        private List<LineRange> outputLineRanges = new List<LineRange>();

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(System.IntPtr windowHandle, int messageCode, int wParam, int[] lParam);

        public static uint TryParseWithDefault(string s, uint defaultValue) { return uint.TryParse(s, out uint value) ? value : defaultValue; }
        public uint MaximumLineLength => TryParseWithDefault(textBoxWrapWidth.Text, DefaultMaximumLineLength);
        public uint LineIndentationPerLevel => TryParseWithDefault(textBoxIndentSize.Text, DefaultLineIndentationPerLevel);

        protected override void WndProc(ref Message m)
        {
            // Show accelerator keys by default.
            if (m.Msg == WM_UPDATEUISTATE)
            {
                m.WParam = (IntPtr)(UIS_CLEAR | (UISF_HIDEACCEL << 16));
            }
            base.WndProc(ref m);
        }

        public formMain()
        {
            InitializeComponent();
            SetTabWidths(this.textBoxTokens, new int[] { 12 * 4, 32 * 4 });
            SetTabWidths(this.textBoxBreakFlags, new int[] { 8 * 4, 12 * 4, 16 * 4 });
            SetTabWidths(this.textBoxLineRanges, new int[] { 12 * 4, 16 * 4, 20 * 4 });
        }

        private void formMain_Load(object sender, EventArgs e)
        {
            ClearTextBoxSelection(textBoxInput);
            ClearTextBoxSelection(textBoxTokens);
            ClearTextBoxSelection(textBoxOutput);
            ClearTextBoxSelection(textBoxBreakFlags);
            ClearTextBoxSelection(textBoxLineRanges);
        }

        private void buttonWrap_Click(object sender, EventArgs e)
        {
            string inputText = textBoxInput.Text;
            uint maximumLineLength = MaximumLineLength;
            uint lineIndentationPerLevel = LineIndentationPerLevel;

            var breakpointOpportunities = GetLineBreakpointOpportunities(inputText, defaultBreakPairTable, defaultCategoryBreakFlags);
            this.lineRanges = GetLineRanges(inputText, breakpointOpportunities, maximumLineLength, lineIndentationPerLevel);
            this.tokenRanges = GetTokenRanges(inputText);
            this.outputLineRanges = GetOutputLineRanges(breakpointOpportunities, lineRanges, lineIndentationPerLevel);

            textBoxTokens.Text = GetTokensText(inputText, this.tokenRanges);
            textBoxBreakFlags.Text = GetBreakFlagsText(inputText, breakpointOpportunities);
            textBoxLineRanges.Text = GetLineRangesText(inputText, breakpointOpportunities, this.lineRanges, LineIndentationPerLevel);
            textBoxOutput.Text = GetWrappedText(inputText, breakpointOpportunities, this.lineRanges, LineIndentationPerLevel);

            ClearTextBoxSelection(textBoxTokens);
            ClearTextBoxSelection(textBoxBreakFlags);
            ClearTextBoxSelection(textBoxLineRanges);
            ClearTextBoxSelection(textBoxOutput);
        }

        private static void ClearTextBoxSelection(TextBox textBox)
        {
            textBox.SelectionStart = 0;
            textBox.SelectionLength = 0;
        }

        public void SetTabWidth(System.Windows.Forms.TextBox textBox, int tabWidth)
        {
            SetTabWidths(textBox, new int[] { tabWidth * 4 });
        }

        public void SetTabWidths(System.Windows.Forms.TextBox textBox, int[] tabWidths)
        {
            SendMessage(textBox.Handle, EM_SETTABSTOPS, tabWidths.Length, tabWidths);
        }

        private void checkBoxWrap_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxWrap.Checked)
            {
                textBoxInput.WordWrap = true;
                textBoxInput.ScrollBars = ScrollBars.Vertical;
            }
            else
            {
                textBoxInput.WordWrap = false;
                textBoxInput.ScrollBars = ScrollBars.Both;
            }
        }

        public static List<LineRangeAndTokenCategory> GetTokenRanges(string inputText)
        {
            List<LineRangeAndTokenCategory> tokenRanges = new List<LineRangeAndTokenCategory>();

            for (uint textPosition = 0; textPosition < inputText.Length;)
            {
                uint previousTextPosition = textPosition;
                TokenCategory category = ReadNextTokenCategory(inputText, ref textPosition);
                tokenRanges.Add(new LineRangeAndTokenCategory { lineRange = new LineRange { start = previousTextPosition, end = textPosition }, tokenCategory = category });
            }
            return tokenRanges;
        }

        public static string GetTokensText(string inputText, List<LineRangeAndTokenCategory> tokenRanges)
        {
            var tokensText = new StringBuilder();

            foreach (var tokenRange in tokenRanges)
            {
                tokensText.Append($"[{tokenRange.lineRange.start}..{tokenRange.lineRange.end})\t");
                tokensText.Append(tokenRange.tokenCategory.ToString());
                tokensText.Append(":\t\"");
                if (tokenRange.tokenCategory != TokenCategory.LineBreak)
                {
                    tokensText.Append(inputText.Substring((int)tokenRange.lineRange.start, (int)(tokenRange.lineRange.Length)));
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

        public static List<LineRange> GetOutputLineRanges(
            TextWrapper.TextWrapper.LineBreakpointOpportunity[] breakpointOpportunities,
            List<LineRange> lineRanges,
            uint lineIndentationPerLevel
            )
        {
            List<LineRange> outputLineRanges = new List<LineRange>();
            uint textStart = 0, textEnd = 0;

            foreach (var lineRange in lineRanges)
            {
                uint indentationLevel = 0;
                bool hasHardLineBreak = false;
                if (lineRange.start < breakpointOpportunities.Length)
                {
                    indentationLevel = breakpointOpportunities[(int)lineRange.start].indentationLevel * lineIndentationPerLevel;
                }
                if (lineRange.end > 0 && lineRange.end - 1 < breakpointOpportunities.Length)
                {
                    hasHardLineBreak = breakpointOpportunities[(int)lineRange.end - 1].MustBreakAfter;
                }
                textStart += indentationLevel;
                textEnd = textStart + lineRange.Length + (hasHardLineBreak ? 0u : 2u);
                outputLineRanges.Add(new LineRange { start = textStart, end = textEnd });
                textStart = textEnd;
            }
            return outputLineRanges;
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
                lineRangesText.Append($"[{lineRange.start}..{lineRange.end})\tx{lineRange.Length}\tL{indentationLevel}\t\"{lineText}\"\r\n");
            }
            return lineRangesText.ToString();
        }

        private void SelectTextBoxTextRange(System.Windows.Forms.TextBox textBox, uint start, uint length)
        {
            textBox.SelectionStart = (int)start;
            textBox.SelectionLength = (int)length;
            textBox.ScrollToCaret();
        }

        private int? FindMatchingLineRangeIndex(List<LineRange> lineRanges, uint textPosition)
        {
            int lineRangeIndex = lineRanges.BinarySearch(
                new LineRange { start = textPosition, end = textPosition },
                Comparer<LineRange>.Create((a, b) => (b.start < a.start) ? 1 : (b.start >= a.end) ? -1 : 0)
            );
            if (lineRangeIndex < 0)
            {
                return null;
            }
            return lineRangeIndex;
        }

        private void textBoxInput_SelectionPotentiallyChanged(object sender, EventArgs e)
        {
            uint textPosition = (uint)textBoxInput.SelectionStart;
            uint textLength = (uint)textBoxInput.SelectionLength;

            if (FindMatchingLineRangeIndex(this.lineRanges, textPosition) is int lineIndex1 &&
                FindMatchingLineRangeIndex(this.lineRanges, textPosition + textLength) is int lineIndex2)
            {
                LineRange inputLineRange1 = (lineIndex1 < this.lineRanges.Count) ? this.lineRanges[lineIndex1] : new LineRange();
                LineRange outputLineRange1 = (lineIndex1 < this.outputLineRanges.Count) ? this.outputLineRanges[lineIndex1] : new LineRange();
                uint outputTextPosition1 = outputLineRange1.start + textPosition - inputLineRange1.start;

                LineRange inputLineRange2 = (lineIndex2 < this.lineRanges.Count) ? this.lineRanges[lineIndex2] : new LineRange();
                LineRange outputLineRange2 = (lineIndex2 < this.outputLineRanges.Count) ? this.outputLineRanges[lineIndex2] : new LineRange();
                uint outputTextPosition2 = outputLineRange2.start + textPosition + textLength - inputLineRange2.start;

                SelectTextBoxTextRange(textBoxOutput, outputTextPosition1, outputTextPosition2 - outputTextPosition1);
            }
        }

        private void textBoxOutput_SelectionPotentiallyChanged(object sender, EventArgs e)
        {
            uint textPosition = (uint)textBoxOutput.SelectionStart;
            uint textLength = (uint)textBoxOutput.SelectionLength;

            if (FindMatchingLineRangeIndex(this.outputLineRanges, textPosition) is int lineIndex1 &&
                FindMatchingLineRangeIndex(this.outputLineRanges, textPosition + textLength) is int lineIndex2)
            {
                LineRange inputLineRange1 = (lineIndex1 < this.lineRanges.Count) ? this.lineRanges[lineIndex1] : new LineRange();
                LineRange outputLineRange1 = (lineIndex1 < this.outputLineRanges.Count) ? this.outputLineRanges[lineIndex1] : new LineRange();
                uint inputTextPosition1 = inputLineRange1.start + textPosition - outputLineRange1.start;

                LineRange inputLineRange2 = (lineIndex2 < this.lineRanges.Count) ? this.lineRanges[lineIndex2] : new LineRange();
                LineRange outputLineRange2 = (lineIndex2 < this.outputLineRanges.Count) ? this.outputLineRanges[lineIndex2] : new LineRange();
                uint inputTextPosition2 = inputLineRange2.start + textPosition + textLength - outputLineRange2.start;
                SelectTextBoxTextRange(textBoxInput, inputTextPosition1, inputTextPosition2 - inputTextPosition1);
            }
        }

        private void textBoxTokens_SelectionPotentiallyChanged(object sender, EventArgs e)
        {
            int tokenLineIndex = textBoxTokens.GetLineFromCharIndex(textBoxTokens.SelectionStart);
            var rangeAndCategory = (tokenLineIndex < this.tokenRanges.Count) ? this.tokenRanges[tokenLineIndex] : new LineRangeAndTokenCategory();
            uint textPosition = rangeAndCategory.lineRange.start;
            uint textLength = rangeAndCategory.lineRange.Length;
            SelectTextBoxTextRange(textBoxInput, textPosition, textLength);

            if (FindMatchingLineRangeIndex(this.lineRanges, textPosition) is int lineIndex)
            {
                LineRange inputLineRange = (lineIndex < this.lineRanges.Count) ? this.lineRanges[lineIndex] : new LineRange();
                LineRange outputLineRange = (lineIndex < this.outputLineRanges.Count) ? this.outputLineRanges[lineIndex] : new LineRange();
                uint outputTextPosition = outputLineRange.start + textPosition - inputLineRange.start;
                SelectTextBoxTextRange(textBoxOutput, outputTextPosition, textLength);
            }
        }

        private void textBoxBreakFlags_SelectionPotentiallyChanged(object sender, EventArgs e)
        {
            uint textPosition = (uint)textBoxBreakFlags.GetLineFromCharIndex(textBoxBreakFlags.SelectionStart);
            uint textLength = 1;
            SelectTextBoxTextRange(textBoxInput, textPosition, textLength);

            if (FindMatchingLineRangeIndex(this.lineRanges, textPosition) is int lineIndex)
            {
                LineRange inputLineRange = (lineIndex < this.lineRanges.Count) ? this.lineRanges[lineIndex] : new LineRange();
                LineRange outputLineRange = (lineIndex < this.outputLineRanges.Count) ? this.outputLineRanges[lineIndex] : new LineRange();
                uint outputTextPosition = outputLineRange.start + textPosition - inputLineRange.start;
                SelectTextBoxTextRange(textBoxOutput, outputTextPosition, textLength);
            }
        }

        private void textBoxLineRanges_SelectionPotentiallyChanged(object sender, EventArgs e)
        {
            int lineIndex = textBoxLineRanges.GetLineFromCharIndex(textBoxLineRanges.SelectionStart);
            LineRange lineRange = (lineIndex < this.lineRanges.Count) ? this.lineRanges[lineIndex] : new LineRange();
            LineRange outputLineRange = (lineIndex < this.outputLineRanges.Count) ? this.outputLineRanges[lineIndex] : new LineRange();
            
            SelectTextBoxTextRange(textBoxInput, lineRange.start, lineRange.Length);
            SelectTextBoxTextRange(textBoxOutput, outputLineRange.start, outputLineRange.Length);
        }
    }
}
