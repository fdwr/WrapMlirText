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
        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(System.IntPtr h, int msg, int wParam, int[] lParam);
        const int WM_UPDATEUISTATE = 0x0128;
        const int UISF_HIDEACCEL = 0x2;
        const int UIS_CLEAR = 0x2;

        public static uint TryParseWithDefault(string s, uint defaultValue) { return uint.TryParse(s, out uint value) ? value : defaultValue; }
        public uint MaximumLineLength => TryParseWithDefault(textBoxWrapWidth.Text, 120);
        public uint LineIndentationPerLevel => TryParseWithDefault(textBoxIndentSize.Text, 4);

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
            SetTabWidth(this.textBoxTokens, 24);
            SetTabWidth(this.textBoxLineRanges, 20);
        }

        private void formMain_Load(object sender, EventArgs e)
        {
            textBoxInput.SelectionStart = 0;
            textBoxInput.SelectionLength = 0;
            textBoxTokens.SelectionStart = 0;
            textBoxTokens.SelectionLength = 0;
            textBoxOutput.SelectionStart = 0;
            textBoxOutput.SelectionLength = 0;
            textBoxBreakFlags.SelectionStart = 0;
            textBoxBreakFlags.SelectionLength = 0;
            textBoxLineRanges.SelectionStart = 0;
            textBoxLineRanges.SelectionLength = 0;
        }

        private void buttonWrap_Click(object sender, EventArgs e)
        {
            string inputText = textBoxInput.Text;
            uint maximumLineLength = MaximumLineLength;
            uint lineIndentationPerLevel = LineIndentationPerLevel;

            var breakpointOpportunities = GetLineBreakpointOpportunities(inputText, breakPairTable, categoryBreakFlags);
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

        private const int EM_SETTABSTOPS = 0x00CB;

        public void SetTabWidth(System.Windows.Forms.TextBox textbox, int tabWidth)
        {
            SendMessage(textbox.Handle, EM_SETTABSTOPS, 1, new int[] { tabWidth * 4 });
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
