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

        private List<LineRange> tokenRanges = new List<LineRange>();
        private List<TokenCategory> tokenCatogories = new List<TokenCategory>();
        private List<LineRange> lineRanges = new List<LineRange>();
        private List<LineRange> outputLineRanges = new List<LineRange>();

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(System.IntPtr windowHandle, int messageCode, int wParam, int[] lParam);

        public static uint TryParseWithDefault(string s, uint defaultValue) { return uint.TryParse(s, out uint value) ? value : defaultValue; }
        public uint MaximumLineLength => TryParseWithDefault(textBoxWrapWidth.Text, DefaultMaximumLineLength);
        public uint LineIndentationPerLevel => TryParseWithDefault(textBoxIndentSize.Text, DefaultLineIndentationPerLevel);

        public struct IntervalUint
        {
            public uint start;
            public uint end;
            public uint Length => end - start;
            public bool IsEmpty => (end <= start);
        }

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
            SetTabStops(this.textBoxTokens, new int[] { 12 * 4, 32 * 4 });
            SetTabStops(this.textBoxBreakFlags, new int[] { 8 * 4, 14 * 4, 18 * 4 });
            SetTabStops(this.textBoxLineRanges, new int[] { 12 * 4, 16 * 4, 20 * 4 });
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
            (this.tokenRanges, this.tokenCatogories) = GetTokenRanges(inputText);
            this.outputLineRanges = GetOutputLineRanges(breakpointOpportunities, lineRanges, lineIndentationPerLevel);

            textBoxTokens.Text = GetTokensText(inputText, this.tokenRanges, this.tokenCatogories);
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

        public void SetTabStop(System.Windows.Forms.TextBox textBox, int tabWidth)
        {
            SetTabStops(textBox, new int[] { tabWidth * 4 });
        }

        public void SetTabStops(System.Windows.Forms.TextBox textBox, int[] tabWidths)
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

        public static (List<LineRange>, List<TokenCategory>) GetTokenRanges(string inputText)
        {
            List<LineRange> tokenRanges = new List<LineRange>();
            List<TokenCategory> tokenCategories = new List<TokenCategory>();

            for (uint textPosition = 0; textPosition < inputText.Length;)
            {
                uint previousTextPosition = textPosition;
                TokenCategory category = ReadNextTokenCategory(inputText, ref textPosition);
                tokenRanges.Add(new LineRange { start = previousTextPosition, end = textPosition });
                tokenCategories.Add(category);
            }
            return (tokenRanges, tokenCategories);
        }

        public static string GetTokensText(string inputText, List<LineRange> tokenRanges, List<TokenCategory> tokenCategories)
        {
            var tokensText = new StringBuilder();

            for (int i = 0; i < tokenRanges.Count; i++)
            {
                var tokenRange = tokenRanges[i];
                var tokenCategory = tokenCategories[i];
                tokensText.Append($"[{tokenRange.start}..{tokenRange.end})\t");
                tokensText.Append(tokenCategory.ToString());
                tokensText.Append(":\t\"");
                if (tokenCategory != TokenCategory.LineBreak)
                {
                    tokensText.Append(inputText.Substring((int)tokenRange.start, (int)(tokenRange.end - tokenRange.start)));
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
                char ch = inputText[(int)textPosition];
                string displayChar = (ch < 32) ? $"\\x{((int)ch):X2}" : ch.ToString();
                breakFlagsText.Append($"[{textPosition}]\t'{displayChar}'\tL{breakpointOpportunities[(int)textPosition].indentationLevel}\t{breakpointOpportunities[(int)textPosition].breakFlags}\r\n");
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

        IntervalUint GetTextBoxSelectedCharacterRange(TextBox textBox)
        {
            int startPosition = textBox.SelectionStart;
            int endPosition = textBox.SelectionStart + textBox.SelectionLength;
            endPosition = Math.Max(endPosition, startPosition);
            return new IntervalUint { start = (uint)startPosition, end = (uint)endPosition };
        }

        IntervalUint GetTextBoxSelectedLineInterval(TextBox textBox)
        {
            var charRange = GetTextBoxSelectedCharacterRange(textBox);
            int startLine = textBox.GetLineFromCharIndex((int)charRange.start);
            int endLine = textBox.GetLineFromCharIndex((int)charRange.end - 1);
            endLine = Math.Max(endLine, startLine);
            return new IntervalUint { start = (uint)startLine, end = (uint)endLine };
        }

        private void SelectTextBoxCharacterRange(System.Windows.Forms.TextBox textBox, uint startPosition, uint endPosition)
        {
            // Silently ignore cases of illegal selections, which can happen if the text boxes are not populated yet.
            if (startPosition < UInt32.MaxValue && endPosition < UInt32.MaxValue)
            {
                // Call ScrollToCaret differently depending on the direction of selection change.
                int start = (int)startPosition;
                int length = (int)(endPosition - startPosition);
                if (startPosition != textBox.SelectionStart)
                {
                    textBox.SelectionStart = start;
                    textBox.SelectionLength = 0;
                    textBox.ScrollToCaret();
                    textBox.SelectionLength = length;
                }
                else
                {
                    textBox.SelectionStart = start;
                    textBox.SelectionLength = length;
                    textBox.ScrollToCaret();
                }
            }
        }

        private void SelectTextBoxLineInterval(System.Windows.Forms.TextBox textBox, uint firstLineIndex, uint lastLineIndex)
        {
            uint startPosition = (uint)textBox.GetFirstCharIndexFromLine((int)firstLineIndex);
            uint endPosition = (uint)textBox.GetFirstCharIndexFromLine((int)lastLineIndex);
            SelectTextBoxCharacterRange(textBox, startPosition, endPosition);
        }

        // Map selected line range to character range.
        IntervalUint GetCharacterRangeFromLineInterval(List<LineRange> lineRanges, IntervalUint lineInterval)
        {
            IntervalUint range = new IntervalUint();
            if (lineRanges.Count > 0)
            {
                lineInterval.end = Math.Min(lineInterval.end, (uint)lineRanges.Count - 1);
                lineInterval.start = Math.Min(lineInterval.start, lineInterval.end);
                range.start = lineRanges[(int)lineInterval.start].start;
                range.end = lineRanges[(int)lineInterval.end].end;
                Debug.Assert(range.start <= range.end);
            }
            return range;
        }

        // Map text position to the line range index that contains it. Return null if not found.
        private uint? FindMatchingLineRangeIndex(List<LineRange> lineRanges, uint textPosition)
        {
            if (lineRanges.Count == 0)
            {
                return null;
            }
            int index = lineRanges.BinarySearch(
                new LineRange { start = textPosition, end = textPosition },
                Comparer<LineRange>.Create((haystack, needle) => (needle.start < haystack.start) ? 1 : (needle.start >= haystack.end) ? -1 : 0)
            );
            if (index < 0) // No exact match found. Return lower bound item (not the insertion point).
            {
                index = ~index;
                index -= (index > 0 ? 1 : 0);
            }
            return (uint)index;
        }

        // Map from one line range to the other, clamping to stay within the ranges.
        uint RemapTextPosition(uint textPosition, uint lineIndex, List<LineRange> inputLineRanges, List<LineRange> outputLineRanges)
        {
            var inputLineRange = inputLineRanges[(int)lineIndex];
            var outputLineRange = outputLineRanges[(int)lineIndex];
            textPosition = Math.Max(textPosition, inputLineRange.start);
            textPosition = Math.Min(textPosition, inputLineRange.end);
            textPosition = outputLineRange.start + (textPosition - inputLineRange.start);
            textPosition = Math.Max(textPosition, outputLineRange.start);
            textPosition = Math.Min(textPosition, outputLineRange.end);
            return textPosition;
        }

        private void UpdateSelections(object sender, IntervalUint inputCharRange)
        {
            if (sender != textBoxInput)
            {
                SelectTextBoxCharacterRange(textBoxInput, inputCharRange.start, inputCharRange.end);
            }

            if (sender != textBoxOutput)
            {
                // Map input character range to output character range.
                if (FindMatchingLineRangeIndex(this.lineRanges, inputCharRange.start) is uint firstLineIndex &&
                    FindMatchingLineRangeIndex(this.lineRanges, inputCharRange.end) is uint lastLineIndex)
                {
                    uint startPosition = RemapTextPosition(inputCharRange.start, firstLineIndex, this.lineRanges, this.outputLineRanges);
                    uint endPosition = RemapTextPosition(inputCharRange.end, lastLineIndex, this.lineRanges, this.outputLineRanges);
                    SelectTextBoxCharacterRange(textBoxOutput, startPosition, endPosition);
                }
            }

            if (sender != textBoxTokens)
            {
                if (FindMatchingLineRangeIndex(this.tokenRanges, inputCharRange.start) is uint firstLineIndex &&
                    FindMatchingLineRangeIndex(this.tokenRanges, inputCharRange.end - Math.Min(inputCharRange.end, 1)) is uint lastLineIndex)
                {
                    lastLineIndex = Math.Max(firstLineIndex + 1, lastLineIndex + 1);
                    SelectTextBoxLineInterval(textBoxTokens, firstLineIndex, lastLineIndex);
                }
            }

            if (sender != textBoxBreakFlags)
            {
                uint firstLineIndex = inputCharRange.start;
                uint lastLineIndex = Math.Max(firstLineIndex + 1, inputCharRange.end);
                SelectTextBoxLineInterval(textBoxBreakFlags, firstLineIndex, lastLineIndex);
            }

            if (sender != textBoxLineRanges)
            {
                if (FindMatchingLineRangeIndex(this.lineRanges, inputCharRange.start) is uint firstLineIndex &&
                    FindMatchingLineRangeIndex(this.lineRanges, inputCharRange.end - Math.Min(inputCharRange.end, 1)) is uint lastLineIndex)
                {
                    lastLineIndex = Math.Max(firstLineIndex + 1, lastLineIndex + 1);
                    SelectTextBoxLineInterval(textBoxLineRanges, firstLineIndex, lastLineIndex);
                }
            }
        }

        private void textBoxInput_SelectionPotentiallyChanged(object sender, EventArgs e)
        {
            var charRange = GetTextBoxSelectedCharacterRange(textBoxInput);
            UpdateSelections(sender, charRange);
        }

        private void textBoxOutput_SelectionPotentiallyChanged(object sender, EventArgs e)
        {
            var outputCharRange = GetTextBoxSelectedCharacterRange(textBoxOutput);

            // Map output character range to input character range.
            if (FindMatchingLineRangeIndex(this.outputLineRanges, outputCharRange.start) is uint firstLineIndex &&
                FindMatchingLineRangeIndex(this.outputLineRanges, outputCharRange.end) is uint lastLineIndex)
            {
                IntervalUint inputCharRange;
                inputCharRange.start = RemapTextPosition(outputCharRange.start, firstLineIndex, this.outputLineRanges, this.lineRanges);
                inputCharRange.end   = RemapTextPosition(outputCharRange.end, lastLineIndex, this.outputLineRanges, this.lineRanges);
                UpdateSelections(sender, inputCharRange);
            }
        }

        private void textBoxTokens_SelectionPotentiallyChanged(object sender, EventArgs e)
        {
            var lineInterval = GetTextBoxSelectedLineInterval((TextBox)sender);
            var charRange = GetCharacterRangeFromLineInterval(this.tokenRanges, lineInterval);
            UpdateSelections(sender, charRange);
        }

        private void textBoxBreakFlags_SelectionPotentiallyChanged(object sender, EventArgs e)
        {
            var lineInterval = GetTextBoxSelectedLineInterval((TextBox)sender);
            lineInterval.end = Math.Max(lineInterval.end + 1, lineInterval.start + 1); // Highlight at least character.
            UpdateSelections(sender, lineInterval);
        }

        private void textBoxLineRanges_SelectionPotentiallyChanged(object sender, EventArgs e)
        {
            var lineInterval = GetTextBoxSelectedLineInterval((TextBox)sender);
            var charRange = GetCharacterRangeFromLineInterval(this.lineRanges, lineInterval);
            UpdateSelections(sender, charRange);
        }
    }
}
