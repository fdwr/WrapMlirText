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
            lineRanges = GetLineRanges(inputText, breakpointOpportunities, maximumLineLength, lineIndentationPerLevel);
            tokenRanges = GetTokenRanges(inputText);

            textBoxTokens.Text = GetTokensText(inputText, tokenRanges);
            textBoxBreakFlags.Text = GetBreakFlagsText(inputText, breakpointOpportunities);
            textBoxLineRanges.Text = GetLineRangesText(inputText, breakpointOpportunities, lineRanges, LineIndentationPerLevel);
            textBoxOutput.Text = GetWrappedText(inputText, breakpointOpportunities, lineRanges, LineIndentationPerLevel);

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

        private void SelectInputTextRange(uint start, uint length)
        {
            textBoxInput.SelectionStart = (int)start;
            textBoxInput.SelectionLength = (int)length;
            textBoxInput.ScrollToCaret();
        }

        private void textBoxBreakFlags_Clicked(object sender, EventArgs e)
        {
            int lineIndex = textBoxBreakFlags.GetLineFromCharIndex(textBoxBreakFlags.SelectionStart);
            SelectInputTextRange((uint)lineIndex, 1);
        }

        private void textBoxTokens_Clicked(object sender, EventArgs e)
        {
            int lineIndex = textBoxTokens.GetLineFromCharIndex(textBoxTokens.SelectionStart);
            var rangeAndCategory = (lineIndex < this.tokenRanges.Count) ? this.tokenRanges[lineIndex] : new LineRangeAndTokenCategory();
            SelectInputTextRange(rangeAndCategory.lineRange.start, rangeAndCategory.lineRange.Length);
        }

        private void textBoxLineRanges_Clicked(object sender, EventArgs e)
        {
            int lineIndex = textBoxLineRanges.GetLineFromCharIndex(textBoxLineRanges.SelectionStart);
            LineRange lineRange = (lineIndex < this.lineRanges.Count) ? this.lineRanges[lineIndex] : new LineRange();
            SelectInputTextRange(lineRange.start, lineRange.Length);
        }
    }
}
