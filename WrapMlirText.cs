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

namespace WrapMlirText
{
    using LboBf = MlirFormatProvider.LineBreakpointOpportunity.BreakFlags;

    public partial class formMain : Form
    {
        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(System.IntPtr h, int msg, int wParam, int[] lParam);

        public const int MaximumLineLength = 120;
        public const int LineIndentationPerLevel = 4;

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
            string tokensText = MlirFormatProvider.GetTokensText(inputText);
            textBoxTokens.Text = tokensText;
            textBoxTokens.SelectionStart = 0; // For some reason, setting the text also selects all the text. So clear it.
            textBoxTokens.SelectionLength = 0;
            string wrappedText = MlirFormatProvider.GetWrappedText(inputText, MaximumLineLength, LineIndentationPerLevel);
            textBoxOutput.Text = wrappedText;
            textBoxOutput.SelectionStart = 0;
            textBoxOutput.SelectionLength = 0;
            string breakFlagsText = MlirFormatProvider.GetBreakFlagsText(inputText);
            textBoxBreakFlags.Text = breakFlagsText;
            textBoxBreakFlags.SelectionStart = 0;
            textBoxBreakFlags.SelectionLength = 0;
            string lineRangesText = MlirFormatProvider.GetLineRangesText(inputText, MaximumLineLength, LineIndentationPerLevel);
            textBoxLineRanges.Text = lineRangesText;
            textBoxLineRanges.SelectionStart = 0;
            textBoxLineRanges.SelectionLength = 0;
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
    }

    // Stupid C#: error CS0116: A namespace cannot directly contain members such as fields, methods or statements
    static public class MlirFormatProvider
    {
        public enum TokenCategory
        {
            None,
            Whitespace,             // U+0020 or Tab
            LineBreak,              // New line characters, such as \n or \r\n
            Comment,                // Either a line comment (//) or a block comment (/* */)
            Identifier,             // Such as variable names, function names, etc. Note '_','$','.' are included too.
            PunctuationOpen,        // Such as (, [, {
            PunctuationClose,       // Such as ), ], }
            PunctuationDelimiter,   // Such as ',', ;, : (not . which must be kept together)
            PunctuationSigil,       // Such as '%', '!', '#',  etc. adorning identifiers
            PunctuationOther,       // Such as +, -, * etc.
            Number,                 // 0-9
            String,                 // Quoted string literal
        }

        //--#pragma warning disable CS0169 // The fields are declared but never used
        public struct LineBreakpointOpportunity
        {
            [Flags]
            public enum BreakFlags : byte
            {
                None               = 0,
                CanBreakAfter      = 0b00000001, // Such as after identifiers, keywords, or certain punctuation.
                ShouldBreakAfter   = 0b00000010, // Such as before/after curly braces if a break occurs
                MustBreakAfter     = 0b00000100, // New lines
                CanSplitAfter      = 0b00001000, // Such as commas, semicolons, or other punctuation that typically separates items in a list.
                IsOpening          = 0b00010000, // Such as opening parentheses, brackets, or braces.
                IsClosing          = 0b00100000, // The character before closing parentheses, brackets, or braces.
                ShouldSplitItems   = 0b01000000, // Split all items in a list onto separate lines if the list exceeds the maximum line length.
                IsInvisible        = 0b10000000, // Space/Tab/CR/LF. These characters do not contribute to ink width of trailing line width.
            }

            public BreakFlags breakFlags;
            public byte indentationLevel;

            public bool CanBreakAfter => (breakFlags & BreakFlags.CanBreakAfter) != 0;
            public bool ShouldBreakAfter => (breakFlags & BreakFlags.ShouldBreakAfter) != 0;
            public bool MustBreakAfter => (breakFlags & BreakFlags.MustBreakAfter) != 0;
            public bool CanSplitAfter => (breakFlags & BreakFlags.CanSplitAfter) != 0;
            public bool IsOpening => (breakFlags & BreakFlags.IsOpening) != 0;
            public bool IsClosing => (breakFlags & BreakFlags.IsClosing) != 0;
            public bool ShouldSplitItems => (breakFlags & BreakFlags.ShouldSplitItems) != 0;
            public bool IsInvisible => (breakFlags & BreakFlags.IsInvisible) != 0;
        }
        //--#pragma warning restore CS0169 // The fields are declared but never used

        // Similar to CharacterRange, but uses half-open intervals which are easier to update
        // than First+Length.
        struct LineRange
        {
            public uint start;
            public uint end;

            public uint Length => end - start;

            public LineRange(uint start, uint end)
            {
                this.start = start;
                this.end = end;
            }
        }

        // Shorter aliases just for table usage.
        const TokenCategory None = TokenCategory.None;
        const TokenCategory Spac = TokenCategory.Whitespace;
        const TokenCategory Brek = TokenCategory.LineBreak;
        const TokenCategory Cmnt = TokenCategory.Comment;
        const TokenCategory Idnt = TokenCategory.Identifier;
        const TokenCategory Open = TokenCategory.PunctuationOpen;
        const TokenCategory Clos = TokenCategory.PunctuationClose;
        const TokenCategory Delm = TokenCategory.PunctuationDelimiter;
        const TokenCategory Sigl = TokenCategory.PunctuationSigil;
        const TokenCategory Othr = TokenCategory.PunctuationOther;
        const TokenCategory Nmbr = TokenCategory.Number;
        const TokenCategory Strg = TokenCategory.String;

        // ASCII character categories for quick lookup.
        // Characters outside the ASCII range are treated as Identifier by default.
        static TokenCategory[] tokenCategories = new TokenCategory[128]
        {
        //              _0    _1    _2    _3    _4    _5    _6    _7    _8    _9    _A    _B    _C    _D    _E    _F
        // 0x00 - 0x0F  NUL   SOH   STX   ETX   EOT   ENQ   ACK   BEL   BS    HT    LF    VT    FF    CR    SO    SI
                        None, None, None, None, None, None, None, None, None, Spac, Brek, Brek, Brek, Brek, None, None,
        // 0x10 - 0x1F  DLE   DC1   DC2   DC3   DC4   NAK   SYN   ETB   CAN   EM    SUB   ESC   FS    GS    RS    US
                        None, None, None, None, None, None, None, None, None, None, None, None, None, None, None, None,
        // 0x20 - 0x2F  Sp    !     "     #     $     %     &     '     (     )     *     +     ,   -     .     / 
                        Spac, Sigl, Strg, Sigl, Sigl, Sigl, Othr, Othr, Open, Clos, Othr, Othr, Delm, Othr, Idnt, Cmnt,
        // 0x30 - 0x3F  0     1     2     3     4     5     6     7     8     9     :     ;     <     =     >     ?
                        Nmbr, Nmbr, Nmbr, Nmbr, Nmbr, Nmbr, Nmbr, Nmbr, Nmbr, Nmbr, Othr, Delm, Open, Othr, Clos, Othr,
        // 0x40 - 0x4F  @     A     B     C     D     E     F     G     H     I     J     K     L     M     N     O
                        Sigl, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt,
        // 0x50 - 0x5F  P     Q     R     S     T     U     V     W     X     Y     Z     [     \     ]     ^     _
                        Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Open, Othr, Clos, Othr, Idnt,
        // 0x60 - 0x6F  `     a     b     c     d     e     f     g     h     i     j     k     l     m     n     o
                        Othr, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt,
        // 0x70 - 0x7F  p     q     r     s     t     u     v     w     x     y     z     {     |     }     ~     DEL
                        Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Open, Othr, Clos, Othr, None,
        };

        const LboBf NoBr = LboBf.None;
        const LboBf CnBr = LboBf.CanBreakAfter;
        const LboBf MsBr = LboBf.MustBreakAfter   | LboBf.CanBreakAfter | LboBf.IsInvisible;
        const LboBf CnSp = LboBf.CanBreakAfter    | LboBf.CanSplitAfter;
        const LboBf NoSp =                          LboBf.CanSplitAfter;
        const LboBf OpBr = LboBf.IsOpening        | LboBf.CanBreakAfter;
        const LboBf OpNo = LboBf.IsOpening;
        const LboBf ClBr = LboBf.IsClosing        | LboBf.CanBreakAfter;
        const LboBf ClNo = LboBf.IsClosing;
        const LboBf WsBr = LboBf.CanBreakAfter    | LboBf.IsInvisible;
        const LboBf WsNo =                          LboBf.IsInvisible;

        // Map adjacent pair of tokens (first,second) to breaking flags.
        // The table corresponds to these rules applied in order:
        //
        // Brek,any  - must break after line break
        // any, Brek - no break before line break
        // None,any  - no break after none
        // any, None - can break after non
        // Open,Clos - no break between empty opening/closing pair
        // Open,any  - can break after opening punctuation
        // any ,Spac - no break before space
        // any ,Open - no break before opening punctuation (custom exception for '{')
        // Spac,any  - can break after space
        // Delm,any  - can split after delimiters
        // any ,Clos - can break after closing punctuation
        // Sigl,Nmbr - no break between sigil punctuation and identifier
        // Sigl,Idnt - no break between sigil punctuation and identifier
        // otherwise - can break anywhere else
        //
        static LboBf[,] breakPairTable = new LboBf[,]
        {
            //           None, Spac, Brek, Cmnt, Idnt, Open, Clos, Delm, Sigl, Othr, Nmbr, Strg,
            //                 ' '   CRLF  //    abc   ({[<   )}]> ,     #%!   +-*   123   "az"
            /* None */  {NoBr, NoBr, NoBr, NoBr, NoBr, NoBr, NoBr, NoBr, NoBr, NoBr, NoBr, NoBr},
            /* Spac */  {WsBr, WsNo, WsNo, WsBr, WsBr, WsBr, WsBr, WsNo, WsBr, WsBr, WsBr, WsBr},
            /* Brek */  {MsBr, MsBr, MsBr, MsBr, MsBr, MsBr, MsBr, MsBr, MsBr, MsBr, MsBr, MsBr},
            /* Cmnt */  {CnBr, NoBr, NoBr, CnBr, CnBr, NoBr, CnBr, NoBr, CnBr, CnBr, CnBr, CnBr},
            /* Idnt */  {CnBr, NoBr, NoBr, CnBr, CnBr, NoBr, CnBr, NoBr, CnBr, CnBr, CnBr, CnBr},
            /* Open */  {OpBr, OpBr, OpNo, OpBr, OpBr, OpBr, OpNo, OpBr, OpBr, OpBr, OpBr, OpBr},
            /* Clos */  {ClBr, ClNo, ClNo, ClBr, ClBr, ClBr, ClBr, ClNo, ClBr, ClBr, ClBr, ClBr},
            /* Delm */  {CnSp, NoSp, NoSp, CnSp, CnSp, CnSp, CnSp, CnSp, CnSp, CnSp, CnSp, CnSp},
            /* Sigl */  {CnBr, NoBr, NoBr, CnBr, NoBr, NoBr, CnBr, NoBr, CnBr, CnBr, NoBr, CnBr},
            /* Othr */  {CnBr, NoBr, NoBr, CnBr, CnBr, CnBr, CnBr, NoBr, CnBr, CnBr, CnBr, CnBr},
            /* Nmbr */  {CnBr, NoBr, NoBr, CnBr, CnBr, CnBr, CnBr, NoBr, CnBr, CnBr, CnBr, CnBr},
            /* Strg */  {CnBr, NoBr, NoBr, CnBr, CnBr, CnBr, CnBr, NoBr, CnBr, CnBr, CnBr, CnBr },
        };

        static Dictionary<char, char> openingClosingPairs = new Dictionary<char, char>
        {
            { '(', ')' },
            { '{', '}' },
            { '[', ']' },
            { '<', '>' },
        };

        public static TokenCategory GetTokenCategory(char currentChar)
        {
            return (currentChar < tokenCategories.Length)
                ? tokenCategories[currentChar]
                : TokenCategory.Identifier;
        }

        public static TokenCategory ReadNextTokenCategory(string text, ref uint textPosition)
        {
            if (textPosition >= text.Length)
            {
                return TokenCategory.None; // End of text
            }

            char currentChar = text[(int)textPosition++];
            TokenCategory tokenCategory = GetTokenCategory(currentChar);

            switch (tokenCategory)
            {
            case TokenCategory.Whitespace:
                SeekAfterWhitespace(text, ref textPosition);
                return TokenCategory.Whitespace;

            case TokenCategory.LineBreak:
                // Check for CR LF sequence.
                if (currentChar == '\r' && textPosition < text.Length && text[(int)textPosition] == '\n')
                {
                    ++textPosition;
                }
                return TokenCategory.LineBreak;

            case TokenCategory.Comment:
                // Check for single line comment "//".
                if (textPosition < text.Length && text[(int)textPosition] == '/')
                {
                    SeekLineUpToLineBreak(text, ref textPosition);
                    return TokenCategory.Comment;
                }
                return TokenCategory.PunctuationOther; // Just a '/' character

            case TokenCategory.Number:
                // Glom all digits, periods, or other identifier characters (e.g. "1.0", "1e10", "2x3").
                while (textPosition < text.Length)
                {
                    currentChar = text[(int)textPosition];
                    tokenCategory = GetTokenCategory(currentChar);
                    if (tokenCategory != TokenCategory.Number &&
                        tokenCategory != TokenCategory.Identifier &&
                        !(currentChar == '.'))
                    {
                        break;
                    }
                    ++textPosition;
                }
                return TokenCategory.Number;

            case TokenCategory.String:
                while (textPosition < text.Length)
                {
                    char nextChar = text[(int)textPosition++];
                    if (nextChar == '\\' && textPosition < text.Length)
                    {
                        // Skip escaped character.
                        textPosition++;
                    }
                    else if (nextChar == '\"')
                    {
                        break; // End of string literal
                    }
                }
                return TokenCategory.String;

            case TokenCategory.Identifier:
                // Found an identifer.
                while (textPosition < text.Length)
                {
                    tokenCategory = GetTokenCategory(text[(int)textPosition]);
                    if (tokenCategory != TokenCategory.Identifier && tokenCategory != TokenCategory.Number)
                    {
                        break;
                    }
                    ++textPosition;
                }
                return TokenCategory.Identifier;

            case TokenCategory.PunctuationOther:
                // Match -> arrow as a single token.
                if (currentChar == '-' && textPosition < text.Length && text[(int)textPosition] == '>')
                {
                    ++textPosition;
                    return TokenCategory.PunctuationOther;
                }
                return TokenCategory.PunctuationOther;

            case TokenCategory.PunctuationSigil:
                // Special dialect resources "#-}" check.
                if (currentChar == '#' && String.CompareOrdinal(text, (int)textPosition - 1, "#-}", 0, 3) == 0)
                {
                    textPosition += 2;
                    return TokenCategory.PunctuationClose;
                }
                return TokenCategory.PunctuationSigil;

            case TokenCategory.PunctuationOpen:
                if (currentChar == '{' && String.CompareOrdinal(text, (int)textPosition - 1, "{-#", 0, 3) == 0)
                {
                    textPosition += 2;
                }
                return TokenCategory.PunctuationOpen;

            case TokenCategory.PunctuationDelimiter:
                return TokenCategory.PunctuationDelimiter;

            default:
                return tokenCategory;
            }
        }

        // Updates the text position to the end of the line (just before any line break)
        // or to the end of the text if no more explicit line breaks are found.
        public static void SeekLineUpToLineBreak(string text, ref uint textPosition)
        {
            while (textPosition < text.Length && GetTokenCategory(text[(int)textPosition++]) != TokenCategory.LineBreak)
            {
            }
        }

        // Updates the text position to the end of the line (just after any line break,
        // meaning the first character of the next line), or to the end of the text if
        // no more explicit line breaks are found.
        public static void SeekAfterWhitespace(string text, ref uint textPosition)
        {
            while (textPosition < text.Length && GetTokenCategory(text[(int)textPosition]) == TokenCategory.Whitespace)
            {
                ++textPosition;
            }
        }

        public static LineBreakpointOpportunity[] AssignLineBreakpointOpportunities(string inputText)
        {
            var breakpointOpportunities = new LineBreakpointOpportunity[inputText.Length];
            if (inputText.Length == 0)
            {
                return breakpointOpportunities;
            }

            uint textPosition = 0;
            var previousTokenCategory = TokenCategory.None;
            var delimiterStack = new List<char>();

            // Dummy for first case when there is no preceding text.
            LineBreakpointOpportunity dummyBreakpointOpportunity = new LineBreakpointOpportunity();
            ref LineBreakpointOpportunity breakpointOpportunity = ref dummyBreakpointOpportunity;

            // Read every pair of adjacent tokens, and assign breaking flags.
            while (textPosition < inputText.Length)
            {
                uint tokenTextPosition = textPosition;
                TokenCategory tokenCategory = ReadNextTokenCategory(inputText, ref textPosition);

                // Assign the breaking flags for this token pair.
                // TODO: Consider another table for the current category to update breaking flags, setting IsOpening and such.
                //--ref LineBreakpointOpportunity breakpointOpportunity = ref (tokenTextPosition > 0) ? ref breakpointOpportunities[(int)tokenTextPosition - 1] : ref dummyBreakpointOpportunity;
                breakpointOpportunity.breakFlags |= breakPairTable[(int)previousTokenCategory, (int)tokenCategory];
                int indentationLevel = delimiterStack.Count;
                LboBf additionalBreakFlags = LboBf.None;

                // Handle any special categories.
                switch (tokenCategory)
                {
                case TokenCategory.Whitespace:
                case TokenCategory.LineBreak:
                    additionalBreakFlags = LboBf.IsInvisible;
                    break;

                case TokenCategory.PunctuationOpen:
                    // Push new level onto the stack.
                    char leadingChar = inputText[(int)tokenTextPosition];
                    if (leadingChar == '{')
                    {
                        breakpointOpportunity.breakFlags |= LboBf.ShouldBreakAfter;
                    }
                    if (leadingChar != '[')
                    {
                        // For '{','<','(', we want to split items onto separate lines if the list exceeds the maximum line length,
                        // but for '[' we want to keep them on the same line if possible since they often contain short lists of
                        // attributes or numbers.
                        breakpointOpportunities[(int)tokenTextPosition].breakFlags |= LboBf.ShouldSplitItems;
                    }
                    delimiterStack.Add(openingClosingPairs[leadingChar]);
                    break;

                case TokenCategory.PunctuationClose:
                    // Pop stack until we find the matching opening delimiter.
                    char trailingChar = inputText[(int)textPosition - 1];
                    while (delimiterStack.Count > 0)
                    {
                        char closingDelimiter = delimiterStack[delimiterStack.Count - 1];
                        delimiterStack.RemoveAt(delimiterStack.Count - 1);
                        if (trailingChar == closingDelimiter)
                        {
                            break;
                        }
                    }
                    indentationLevel = delimiterStack.Count;
                    break;
                }

                // Set the indentation level for whole range of characters in the current token read.
                byte cappedIndentationLevel = (byte)Math.Min(255, indentationLevel);
                for (uint i = tokenTextPosition; i < textPosition; i++)
                {
                    ref var currentBreakpoint = ref breakpointOpportunities[(int)i];
                    currentBreakpoint.indentationLevel = cappedIndentationLevel;
                    currentBreakpoint.breakFlags |= additionalBreakFlags;
                }

                // Update for next round.
                previousTokenCategory = tokenCategory;
                breakpointOpportunity = ref breakpointOpportunities[(int)textPosition - 1];
            }

            // Ensure there's always a breakpoint at the end of the text (can simplify other logic later).
            breakpointOpportunities[breakpointOpportunities.Length - 1].breakFlags |= LboBf.CanBreakAfter;

            return breakpointOpportunities;
        }

        static bool SplitLineRanges(List<LineRange> lineRanges, int lineIndex, uint breakPosition)
        {
            Debug.Assert(lineRanges != null);
            Debug.Assert(lineIndex < lineRanges.Count);

            // If the split point is not within the line range, treat it as a nop
            // (simplifying calling code so it doesn't need to check).
            LineRange lineRange = lineRanges[lineIndex];
            Debug.Assert(lineRange.end > lineRange.start);
            if (breakPosition > lineRange.start && breakPosition < lineRange.end)
            {
                lineRanges.Insert(lineIndex, new LineRange(lineRange.start, breakPosition));
                lineRanges[lineIndex + 1] = new LineRange(breakPosition, lineRange.end);
                return true;
            }
            return false;
        }

        static bool IsLineBreakAdjacent(
            LineBreakpointOpportunity[] breakpointOpportunities,
            uint textPosition,
            bool lookBackward // Seek backward rather than forward
            )
        {
            var breakpointLength = breakpointOpportunities.Length;
            while (true)
            {
                if (lookBackward ? (textPosition-- == 0) : (textPosition >= breakpointLength))
                {
                    break;
                }

                LineBreakpointOpportunity breakpoint = breakpointOpportunities[textPosition];
                if (breakpoint.MustBreakAfter)
                {
                    return true;
                }
                else if (!breakpoint.IsInvisible)
                {
                    return false;
                }

                if (!lookBackward)
                {
                    ++textPosition;
                }
            }
            return false;
        }

        static List<LineRange> GetLineRanges(
            string inputText,
            LineBreakpointOpportunity[] breakpointOpportunities,
            uint maximumLineLength,
            uint lineIndentationPerLevel
            )
        {
            // Add the entire text as one large initial potential line.
            var lineRanges = new List<LineRange>();
            lineRanges.Add(new LineRange(0, (uint)inputText.Length));

            // Find the breakpoint in every line, splitting line ranges.
            for (int lineIndex = 0; lineIndex < lineRanges.Count; ++lineIndex)
            {
                LineRange lineRange = lineRanges[lineIndex];

                // Skip any existing leading whitespace, since that will be replaced by indentation anyway.
                SeekAfterWhitespace(inputText, ref lineRange.start);
                lineRange.start = Math.Min(lineRange.end, lineRange.start);
                if (lineRange.start >= inputText.Length)
                {
                    break;
                }
                lineRanges[lineIndex] = lineRange; // Update lest whitespace skipped.

                uint breakPosition = lineRange.start;
                uint candidateBreakPosition = lineRange.start;
                uint baseIndentationLevel = breakpointOpportunities[(int)lineRange.start].indentationLevel;
                uint minimumIndentationLevel = baseIndentationLevel;
                uint indentation = baseIndentationLevel * lineIndentationPerLevel;
                uint lineLength = indentation;

                // Find the rightmost breakpoint candidate position that fits within the maximum line length.
                for (uint textPosition = lineRange.start; textPosition < lineRange.end; /*increment in loop*/)
                {
                    var opportunity = breakpointOpportunities[(int)textPosition++];
                    ++lineLength;

                    // Break immediately if a hard line break, regardless of line width.
                    // e.g. [---------------------]
                    //      Hello world CR LF My name is Inigo...
                    //                      |
                    if (opportunity.MustBreakAfter)
                    {
                        breakPosition = textPosition;
                        break;
                    }

                    // Break if the line length is too long, but only if there's at least one breakpoint candidate
                    // found so far, and we're not just within trailing whitespace.
                    //
                    // e.g. [--------------]
                    //      Hello there world. My name is Inigo...
                    //                 /\    | (stop after "world", but keep breakpoint after "there ")
                    if (lineLength > maximumLineLength && candidateBreakPosition > lineRange.start && !opportunity.IsInvisible)
                    {
                        breakPosition = candidateBreakPosition;
                        break;
                    }

                    // Look for candidate breakpoints at the right nesting level.
                    // Skip over any breaks that are within a more deeply nested delimiter,
                    // and any dips in levels become the new minimum level.
                    //
                    // e.g. [-------------]
                    //      Hello (there dear) world
                    //            /\---------> (skip over "there dear")
                    if (opportunity.indentationLevel < minimumIndentationLevel)
                    {
                        minimumIndentationLevel = opportunity.indentationLevel;
                    }
                    if (opportunity.indentationLevel == minimumIndentationLevel && opportunity.CanBreakAfter)
                    {
                        // Record this candidate break, but continue looking for a potentially later one.
                        candidateBreakPosition = textPosition;
                    }
                }

                string temp1 = inputText.Substring((int)lineRange.start, (int)lineRange.Length);
                string temp2 = inputText.Substring((int)lineRange.start, (int)(breakPosition - lineRange.start));

                // If no candidate breakpoints were found, jump to the end of the known line range for some forward progress.
                // This could happen with a really long word.
                if (breakPosition == lineRange.start)
                {
                    breakPosition = lineRange.end;
                }
                else // Split the current line at the candidate breakpoint.
                {
                    SplitLineRanges(lineRanges, lineIndex, breakPosition);
                }

                // TODO: Don't break if the breakpoint plus indentation would be less.
                // That is, the break would not improve anything, such as "a bcdefg" with a narrow wrap, where 4 columns would shove bcdefg over further.

                // Split the opening/closing dividers and potentially all subitems separated by commas. e.g.
                //
                //      config<capabilities = {}, subconfig = {}, alignment = {}>
                //            /\                /\              /\             /\
                if (breakPosition > 0 &&
                    breakPosition < breakpointOpportunities.Length &&
                    (breakpointOpportunities[(int)breakPosition].indentationLevel > minimumIndentationLevel ||
                    breakpointOpportunities[(int)breakPosition - 1].indentationLevel > minimumIndentationLevel))
                {
                    // Look backward for the opening punctuation to see if is flagged as wanting items to be split. e.g.
                    //
                    //      config<capabilities = {}, subconfig = {}, alignment = {}>
                    //            |<---- Yes, flag says to split items (each comma separated key value pair).
                    //
                    //      values = [1,2,3,4,5,6,7,8,9,10]
                    //               |<---- No, retain items because splitting every value would consume many lines.
                    bool shouldSplitItems = false;
                    for (uint textPosition = breakPosition; textPosition-- > lineRange.start; )
                    {
                        var breakpoint = breakpointOpportunities[(int)textPosition];
                        if (breakpoint.IsOpening && breakpoint.indentationLevel == minimumIndentationLevel)
                        {
                            // TODO: Look for trailing comment. e.g. "someParameter, // comment"
                            //       to avoid splitting between the previous content.

                            // Check if we should break before the previous opening punctuation. e.g.
                            //
                            //      someScope { someText moreLongTextHere }.
                            //               /\<---- yes, break before curly braces.
                            //
                            //      function(parameterOne, parameterTwo)
                            //             /\<---- no, keep parentheses on same line as call.
                            // TODO: Check with {-# #-}
                            if (textPosition > 0 &&
                                breakpointOpportunities[(int)textPosition - 1].ShouldBreakAfter &&
                                !IsLineBreakAdjacent(breakpointOpportunities, textPosition, lookBackward: true))
                            {
                                if (SplitLineRanges(lineRanges, lineIndex, textPosition))
                                {
                                    ++lineIndex;
                                }
                            }
                            // Break after the opening punctuation.
                            //
                            //      someScope { someText moreLongTextHere }.
                            //                /\<---- break after curly braces.
                            //
                            //      function(parameterOne, parameterTwo)
                            //              /\<---- break after parentheses to separate parameters.
                            // TODO: Check with {-# #-}
                            if (!IsLineBreakAdjacent(breakpointOpportunities, textPosition + 1, lookBackward: false))
                            {
                                SplitLineRanges(lineRanges, lineIndex, textPosition + 1);
                            }
                            shouldSplitItems = breakpoint.ShouldSplitItems;
                            break;
                        }
                    }

                    for (int nextLineIndex = lineIndex + 1; nextLineIndex < lineRanges.Count; ++nextLineIndex)
                    {
                        LineRange nextLineRange = lineRanges[nextLineIndex];
                        for (uint textPosition = nextLineRange.start; textPosition < nextLineRange.end; textPosition++)
                        {
                            var opportunity = breakpointOpportunities[(int)textPosition];
                            if (opportunity.indentationLevel <= minimumIndentationLevel)
                            {
                                // Break closing punctuation, unless there's already a line break. e.g.
                                //
                                //      someScope { someText moreLongTextHere }.
                                //                                           |<---- break
                                if (!IsLineBreakAdjacent(breakpointOpportunities, textPosition, lookBackward: true))
                                {
                                    SplitLineRanges(lineRanges, nextLineIndex, textPosition);
                                }
                                break; // Exited the nested scope, such as after } ) >.
                            }
                            else if (
                                shouldSplitItems &&
                                opportunity.indentationLevel == minimumIndentationLevel + 1 &&
                                opportunity.CanSplitAfter &&
                                !IsLineBreakAdjacent(breakpointOpportunities, textPosition + 1, lookBackward: false)
                                )
                            {
                                uint delimiterBreakPosition = textPosition + 1;
                                SplitLineRanges(lineRanges, nextLineIndex, delimiterBreakPosition);
                            }
                        } // for textPosition
                    } // for nextLineIndex
                } // if split items
            }

            return lineRanges;
        }

        public static string GetTokensText(string inputText)
        {
            var tokensText = new StringBuilder();

            for (uint textPosition = 0; textPosition < inputText.Length;)
            {
                uint previousTextPosition = textPosition;
                TokenCategory category = ReadNextTokenCategory(inputText, ref textPosition);
                tokensText.Append(category.ToString());
                tokensText.Append(":\t");
                if (category != TokenCategory.LineBreak)
                {
                    tokensText.Append(inputText.Substring((int)previousTextPosition, (int)(textPosition - previousTextPosition)));
                }
                tokensText.Append("\r\n");
            }
            return tokensText.ToString();
        }

        public static string GetBreakFlagsText(string inputText)
        {
            LineBreakpointOpportunity[] breakpointOpportunities = AssignLineBreakpointOpportunities(inputText);

            var breakFlagsText = new StringBuilder();

            for (uint textPosition = 0; textPosition < inputText.Length; ++textPosition)
            {
                breakFlagsText.Append($"[{textPosition}]\t'{inputText[(int)textPosition]}'\tL{breakpointOpportunities[(int)textPosition].indentationLevel}\t{breakpointOpportunities[(int)textPosition].breakFlags}\r\n");
            }
            return breakFlagsText.ToString();
        }

        public static string GetLineRangesText(
            string inputText,
            uint maximumLineLength,
            uint lineIndentationPerLevel
            )
        {
            var breakpointOpportunities = AssignLineBreakpointOpportunities(inputText);
            var lineRanges = GetLineRanges(inputText, breakpointOpportunities, maximumLineLength, lineIndentationPerLevel);
            var lineRangesText = new StringBuilder();

            foreach (var lineRange in lineRanges)
            {
                string lineText = inputText.Substring((int)lineRange.start, (int)lineRange.Length);
                if (lineText.EndsWith("\r\n"))
                {
                    lineText = lineText.Remove((int)lineText.Length - 2);
                }
                var indentationLevel = (lineRange.start < breakpointOpportunities.Length) ? breakpointOpportunities[(int)lineRange.start].indentationLevel : 0;
                lineRangesText.Append($"@{lineRange.start}..{lineRange.end} x{lineRange.Length} L{indentationLevel}\t{lineText}\r\n");
            }
            return lineRangesText.ToString();
        }

        public static string GetWrappedText(
            string inputText,
            uint maximumLineLength,
            uint lineIndentationPerLevel
            )
        {
            if (string.IsNullOrEmpty(inputText))
            {
                return String.Empty; // Nothing to wrap.
            }

            var breakpointOpportunities = AssignLineBreakpointOpportunities(inputText);
            var lineRanges = GetLineRanges(inputText, breakpointOpportunities, maximumLineLength, lineIndentationPerLevel);
            var wrappedText = new StringBuilder();

            // Concatenate the lines together, inserting additional line breaks as needed.
            foreach (var lineRange in lineRanges)
            {
                uint lineIndentationLevel = lineRange.start < breakpointOpportunities.Length ? breakpointOpportunities[(int)lineRange.start].indentationLevel : 0u;
                uint indentation = lineIndentationLevel * lineIndentationPerLevel;
                wrappedText.Append(' ', (int)indentation);

                wrappedText.Append(inputText.Substring((int)lineRange.start, (int)lineRange.Length));
                // TODO: Set MustBreakAfter on last item if CR LF.
                //if (!breakpointOpportunities[lineRange.end - 1].MustBreakAfter)
                //{
                //    wrappedText.Append("\r\n");
                //}
                if (GetTokenCategory(inputText[(int)lineRange.end - 1]) != TokenCategory.LineBreak)
                {
                    wrappedText.Append("\r\n");
                }
            }

            return wrappedText.ToString();
        }
    }
}
