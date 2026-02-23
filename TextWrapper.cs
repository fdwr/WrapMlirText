using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace TextWrapper
{
    using LboBf = TextWrapper.LineBreakpointOpportunity.BreakFlags;

    // Stupid C#: error CS0116: A namespace cannot directly contain members such as fields, methods or statements
    static public class TextWrapper
    {
        public enum TokenCategory
        {
            None,
            Whitespace,             // U+0020 or Tab
            LineBreak,              // New line characters, such as \r, \n, or \r\n
            Comment,                // Either a line comment (//) or a block comment (/* */)
            Identifier,             // Variable names, function names, etc. Note '_','$','.' are included too.
            PunctuationOpen,        // (, [, {, <
            PunctuationClose,       // ), ], }, >
            PunctuationDelimiter,   // Item dividers like ',', ;.
            PunctuationSigil,       // Identifier sigils like '%','#','@','^','!'
            PunctuationOther,       // Such as +, -, * etc.
            Number,                 // 0-9
            String,                 // Quoted string literal
        }

        public struct LineBreakpointOpportunity
        {
            // Some of these breaking flags apply to the entire token's character range, while others only apply to the tail end
            // of the token (e.g. the break opportunities after the code unit).
            [Flags]
            public enum BreakFlags : byte
            {
                None               = 0,
                CanBreakAfter      = 0b00000001, // Break after identifiers, keywords, or certain punctuation (no break if the line fits).
                ShouldBreakAfter   = 0b00000010, // Should break such as before curly braces *when* a break occurs (no break if the line fits).
                MustBreakAfter     = 0b00000100, // Hard break characters like CR, LF, CR+LF.
                CanSplitAfter      = 0b00001000, // A delimiter (comma, semicolon...) that typically separates items in a list (pertinent if ShouldSplitItems true).
                IsOpening          = 0b00010000, // Opening parentheses, brackets, or braces.
                IsClosing          = 0b00100000, // Closing parentheses, brackets, or braces.
                ShouldSplitItems   = 0b01000000, // Combined with IsOpening, logic should split all delimited subitems to separate lines when item list is too long.
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

        // Line range using starting and ending text positions, which is similar to System.Drawing.CharacterRange,
        // except it uses half-open intervals which are easier to update and split than First + Length.
        public struct LineRange
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

        // Shorter aliases for more compact table usage.
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

        // Character categories for quick lookup.
        // Characters outside the ASCII range are treated as Identifier by default.
        // MLIR doesn't appear to support Unicode identifiers anyway.
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
                        Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Open, Othr, Clos, Sigl, Idnt,
        // 0x60 - 0x6F  `     a     b     c     d     e     f     g     h     i     j     k     l     m     n     o
                        Othr, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt,
        // 0x70 - 0x7F  p     q     r     s     t     u     v     w     x     y     z     {     |     }     ~     DEL
                        Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Idnt, Open, Othr, Clos, Othr, None,
        };

        const LboBf BfNo /* no break / glue      */ = LboBf.None;
        const LboBf BfCn /* can break            */ = LboBf.CanBreakAfter;
        const LboBf BfLb /* hard line break      */ = LboBf.CanBreakAfter | LboBf.MustBreakAfter;
        const LboBf BfDe /* delimiter with break */ = LboBf.CanBreakAfter | LboBf.CanSplitAfter;
        const LboBf BfDn /* delimiter no break   */ =                       LboBf.CanSplitAfter;

        // Map adjacent pair of tokens (previous,next) to breaking flags.
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
            /* None */  {BfNo, BfNo, BfNo, BfNo, BfNo, BfNo, BfNo, BfNo, BfNo, BfNo, BfNo, BfNo},
            /* Spac */  {BfCn, BfNo, BfNo, BfCn, BfCn, BfCn, BfCn, BfNo, BfCn, BfCn, BfCn, BfCn},
            /* Brek */  {BfLb, BfLb, BfLb, BfLb, BfLb, BfLb, BfLb, BfLb, BfLb, BfLb, BfLb, BfLb},
            /* Cmnt */  {BfCn, BfNo, BfNo, BfCn, BfCn, BfNo, BfCn, BfNo, BfCn, BfCn, BfCn, BfCn},
            /* Idnt */  {BfCn, BfNo, BfNo, BfCn, BfCn, BfNo, BfCn, BfNo, BfCn, BfCn, BfCn, BfCn},
            /* Open */  {BfCn, BfCn, BfNo, BfCn, BfCn, BfCn, BfNo, BfCn, BfCn, BfCn, BfCn, BfCn},
            /* Clos */  {BfCn, BfNo, BfNo, BfCn, BfCn, BfCn, BfCn, BfNo, BfCn, BfCn, BfCn, BfCn},
            /* Delm */  {BfDe, BfDn, BfDn, BfDe, BfDe, BfDe, BfDe, BfDe, BfDe, BfDe, BfDe, BfDe},
            /* Sigl */  {BfCn, BfNo, BfNo, BfCn, BfNo, BfNo, BfCn, BfNo, BfCn, BfCn, BfNo, BfCn},
            /* Othr */  {BfCn, BfNo, BfNo, BfCn, BfCn, BfCn, BfCn, BfNo, BfCn, BfCn, BfCn, BfCn},
            /* Nmbr */  {BfCn, BfNo, BfNo, BfCn, BfCn, BfCn, BfCn, BfNo, BfCn, BfCn, BfCn, BfCn},
            /* Strg */  {BfCn, BfNo, BfNo, BfCn, BfCn, BfCn, BfCn, BfNo, BfCn, BfCn, BfCn, BfCn},
        };

        const LboBf BfIn = LboBf.IsInvisible;
        const LboBf BfOp = LboBf.IsOpening;
        const LboBf BfCl = LboBf.IsClosing;

        // Map current token category to breaking flags which apply to the entire token's character range,
        // such as IsOpening or IsInvisible.
        //
        static LboBf[] categoryBreakFlags = new LboBf[]
        {
            //  None, Spac, Brek, Cmnt, Idnt, Open, Clos, Delm, Sigl, Othr, Nmbr, Strg,
            //        ' '   CRLF  //    abc   ({[<   )}]> ,     #%!   +-*   123   "az"
                BfNo, BfIn, BfIn, BfNo, BfNo, BfOp, BfCl, BfNo, BfNo, BfNo, BfNo, BfNo,
        };

        // Mirror pairs for nesting stack matching.
        static Dictionary<char, char> openingClosingPairs = new Dictionary<char, char>
        {
            { '(', ')' },
            { '{', '}' },
            { '[', ']' },
            { '<', '>' },
        };

        public static TokenCategory GetTokenCategory(char currentChar)
        {
            return (currentChar < tokenCategories.Length) ? tokenCategories[currentChar] : TokenCategory.Identifier;
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
                    char nextChar = text[(int)textPosition];
                    tokenCategory = GetTokenCategory(nextChar);
                    if (tokenCategory != TokenCategory.Number &&
                        tokenCategory != TokenCategory.Identifier &&
                        !(nextChar == '.'))
                    {
                        break;
                    }
                    ++textPosition;
                }
                return TokenCategory.Number;

            case TokenCategory.String:
                // Find the end of the string, checking for \" to avoid ending early.
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
                // Consume the entire identifier and any inline numbers, plus infix characters like '_' and '.',
                // but not other sigils like #, %, !.
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
                // Match "->" arrow as a single token.
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

            default:
                return tokenCategory;
            }
        }

        // Moves the text position to the end of the line (just before any line break, but not consuming it)
        // or to the end of the text if no more explicit line breaks are found.
        public static void SeekLineUpToLineBreak(string text, ref uint textPosition)
        {
            for (; textPosition < text.Length && GetTokenCategory(text[(int)textPosition]) != TokenCategory.LineBreak; ++textPosition)
            {
            }
        }

        // Moves the text position to the end of any whitespace, meaning the first character afterward, or to the end
        // of the text if no more explicit line breaks are found.
        public static void SeekAfterWhitespace(string text, ref uint textPosition)
        {
            for (; textPosition < text.Length && GetTokenCategory(text[(int)textPosition]) == TokenCategory.Whitespace; ++textPosition)
            {
            }
        }

        // Return an array of breakpoint opportunities for each code unit in the text.
        // This stage is agnostic to maximum line wrap width, purely based on text properties and adjacent token pairs.
        public static LineBreakpointOpportunity[] AssignLineBreakpointOpportunities(string inputText)
        {
            var breakpointOpportunities = new LineBreakpointOpportunity[inputText.Length];
            if (inputText.Length == 0)
            {
                return breakpointOpportunities;
            }

            var previousTokenCategory = TokenCategory.None;
            var delimiterStack = new List<char>();

            // Dummy breakpoint for first case when there is no preceding text.
            LineBreakpointOpportunity dummyBreakpointOpportunity = new LineBreakpointOpportunity();
            ref LineBreakpointOpportunity previousBreakpointOpportunity = ref dummyBreakpointOpportunity;

            // Read every pair of adjacent tokens, and assign breaking flags. e.g.
            //
            //      sigil      x identifier = glue (no break)
            //      identifier x identifier = can break
            //      line break x identifier = must break
            for (uint textPosition = 0; textPosition < inputText.Length; /*increment inside*/)
            {
                uint currentTokenStartPosition = textPosition;
                TokenCategory currentTokenCategory = ReadNextTokenCategory(inputText, ref textPosition);
                Debug.Assert(textPosition > currentTokenStartPosition); // Every token should be at least one character.
                uint currentTokenLastPosition = textPosition - 1;

                // Assign the breaking flags for this token pair together and current token.
                previousBreakpointOpportunity.breakFlags |= breakPairTable[(int)previousTokenCategory, (int)currentTokenCategory];
                LboBf currentBreakOpportunityFlags = categoryBreakFlags[(int)currentTokenCategory];
                int indentationLevel = delimiterStack.Count;

                // Handle any special cases, mainly opening/closing punctuation.
                switch (currentTokenCategory)
                {
                case TokenCategory.PunctuationOpen:
                    // Push new level onto the stack.
                    char leadingChar = inputText[(int)currentTokenStartPosition];
                    if (leadingChar == '{')
                    {
                        previousBreakpointOpportunity.breakFlags |= LboBf.ShouldBreakAfter;
                    }
                    if (leadingChar != '[')
                    {
                        // For '{','<','(', we want to split items onto separate lines if the list exceeds the maximum line length,
                        // but for '[' we want to keep them on the same line if possible since they often contain short lists of
                        // attributes or numbers.
                        breakpointOpportunities[(int)currentTokenStartPosition].breakFlags |= LboBf.ShouldSplitItems;
                    }
                    delimiterStack.Add(openingClosingPairs[leadingChar]);
                    break;

                case TokenCategory.PunctuationClose:
                    // Pop stack until we find the matching opening delimiter.
                    // Well formed text should always match on the first try, but we can be resilient to malformed text
                    // by allowing mismatches and just popping until we find a match or run out of stack.
                    char trailingChar = inputText[(int)currentTokenLastPosition];
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

                // Update the next breakpoint opportunity's indentation level and flags for whole code unit range of
                // characters in the current token (not just the tail end).
                byte cappedIndentationLevel = (byte)Math.Min(255, indentationLevel);
                for (uint i = currentTokenStartPosition; i < textPosition; i++)
                {
                    ref var currentBreakpoint = ref breakpointOpportunities[(int)i];
                    currentBreakpoint.indentationLevel = cappedIndentationLevel;
                    currentBreakpoint.breakFlags |= currentBreakOpportunityFlags;
                }

                previousTokenCategory = currentTokenCategory;
                previousBreakpointOpportunity = ref breakpointOpportunities[(int)currentTokenLastPosition];
            }

            // Flush the last breakpoint opportunity, which will be the only one that doesn't get updated by the loop since
            // there is no next token to trigger the update. Also ensure there's always a breakpoint at the end of the text,
            // which can simplify later logic.
            previousBreakpointOpportunity.breakFlags |= breakPairTable[(int)previousTokenCategory, (int)TokenCategory.None];
            previousBreakpointOpportunity.breakFlags |= LboBf.CanBreakAfter;

            return breakpointOpportunities;
        }

        static bool SplitLineRanges(List<LineRange> lineRanges, int lineIndex, uint breakPosition)
        {
            Debug.Assert(lineRanges != null);
            Debug.Assert(lineIndex < lineRanges.Count);

            // If the split point is not within the line range or at the edges, treat it as a nop
            // (simplifying calling code so it doesn't need to check).
            LineRange lineRange = lineRanges[lineIndex];
            Debug.Assert(lineRange.end >= lineRange.start, "Empty LineRange's are okay, but not inverted ones.");

            if (breakPosition > lineRange.start && breakPosition < lineRange.end)
            {
                lineRanges.Insert(lineIndex, new LineRange(lineRange.start, breakPosition));
                lineRanges[lineIndex + 1] = new LineRange(breakPosition, lineRange.end);
                return true;
            }
            return false;
        }

        enum LookDirection
        {
            Forward,
            Backward,
        };

        // See if a line break is adjacent to the given text position in the given direction,
        // ignoring any invisible whitespace characters in between. This is useful to avoid
        // splitting a line even further when there's already an explicit line break. e.g.
        //
        //      "Hello world{CR}{LF} My name is Inigo..."
        //                 /\---> yes, line break is adjacent to "d" (text position 11)
        //      "Hello world   {CR}{LF} My name is Inigo..."
        //                 /\---> yes, line break is adjacent to "d" skipping over spaces (text position 11)
        //      "Hello world{CR}{LF} My name is Inigo..."
        //           /\---> no, line break is not adjacent to "o" (text position 5)
        //
        static bool IsLineBreakAdjacent(
            LineBreakpointOpportunity[] breakpointOpportunities,
            uint textPosition,
            LookDirection lookDirection
            )
        {
            var breakpointsLength = breakpointOpportunities.Length;
            bool lookForward = (lookDirection == LookDirection.Forward);

            while (lookForward ? (textPosition < breakpointsLength) : (textPosition-- != 0))
            {
                LineBreakpointOpportunity breakpoint = breakpointOpportunities[textPosition];
                if (breakpoint.MustBreakAfter)
                {
                    return true;
                }
                else if (!breakpoint.IsInvisible)
                {
                    return false;
                }

                if (lookForward)
                {
                    ++textPosition;
                }
            }
            return false;
        }

        // Collect all the ranges per line, splitting based on the maximum line length.
        public static List<LineRange> GetLineRanges(
            string inputText,
            LineBreakpointOpportunity[] breakpointOpportunities,
            uint maximumLineLength,
            uint lineIndentationPerLevel
            )
        {
            // Add the entire text as one large initial potential line.
            // This serves as a pending queue of lines to process and the final result.
            var lineRanges = new List<LineRange>();
            lineRanges.Add(new LineRange(0, (uint)inputText.Length));

            // Find the best breakpoint in every line, splitting once found.
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
                lineRanges[lineIndex] = lineRange; // Update range lest whitespace was skipped.

                uint breakPosition = lineRange.start; // Actual break position to split.
                uint candidateBreakPosition = lineRange.start; // Best candidate so far.
                uint firstIndentationLevel = breakpointOpportunities[(int)lineRange.start].indentationLevel;
                uint minimumIndentationLevel = firstIndentationLevel;
                uint indentation = firstIndentationLevel * lineIndentationPerLevel;
                uint lineLength = indentation;

                // Find the rightmost breakpoint candidate position that fits within the maximum line length.
                for (uint textPosition = lineRange.start; textPosition < lineRange.end; /*increment in loop*/)
                {
                    var opportunity = breakpointOpportunities[(int)textPosition++];
                    ++lineLength;

                    // Break immediately if a hard line break, regardless of line width.
                    // e.g. [---------------------]
                    //      "Hello world{CR}{LF}My name is Inigo..."
                    //                         /\ <---- Break after LF (not CR).
                    //
                    if (opportunity.MustBreakAfter)
                    {
                        breakPosition = textPosition;
                        break;
                    }

                    // Break if the line length is too long, but only if there's at least one breakpoint candidate
                    // found so far, and we're not just within trailing whitespace.
                    //
                    // e.g. [--------------]
                    //      "Hello there world. My name is Inigo..."
                    //                  /\    | <---- Exit loop after "world", but keep earlier candidate breakpoint after "there ".
                    //
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
                    //      "Hello (there dear) world"
                    //            /\<---- Skip over "there dear", keeping "Hello " as candidate breakpoint since it's at the right level.
                    //
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

                // If no candidate breakpoints were found, jump to the end of the known line range for some forward progress.
                // This might happen with a really long word.
                if (breakPosition == lineRange.start)
                {
                    breakPosition = lineRange.end;
                }
                else // Split the current line at the candidate breakpoint.
                {
                    SplitLineRanges(lineRanges, lineIndex, breakPosition);
                }

                // Split items inside level changes due to opening/closing puncuation.
                if (breakPosition > 0 &&
                    breakPosition < breakpointOpportunities.Length &&
                    (breakpointOpportunities[(int)breakPosition].indentationLevel > minimumIndentationLevel ||
                    breakpointOpportunities[(int)breakPosition - 1].indentationLevel > minimumIndentationLevel))
                {
                    SplitOpeningClosingDelimiters(breakpointOpportunities, lineRanges, lineIndex, breakPosition, minimumIndentationLevel);
                }
            }

            return lineRanges;
        }

        // Split the opening/closing dividers and potentially all subitems separated by commas. e.g.
        //
        //      config<capabilities = {}, subconfig = {}, alignment = {}>
        //            /\                /\              /\             /\
        //
        private static void SplitOpeningClosingDelimiters(
            LineBreakpointOpportunity[] breakpointOpportunities,
            List<LineRange> lineRanges,
            int lineIndex,
            uint breakPosition,
            uint minimumIndentationLevel
            )
        {
            LineRange lineRange = lineRanges[lineIndex];

            // Look backward for the opening punctuation to see if is flagged as wanting items to be split. e.g.
            //
            //      config<capabilities = {}, subconfig = {}, alignment = {}>
            //            |<---- Yes, flag says to split items (each comma separated key value pair).
            //
            //      values = [1,2,3,4,5,6,7,8,9,10]
            //               |<---- No, retain items because splitting every value would consume many lines.
            //
            bool shouldSplitDelimitedItems = false;
            for (uint textPosition = breakPosition; textPosition-- > lineRange.start; )
            {
                var breakpoint = breakpointOpportunities[(int)textPosition];
                if (!breakpoint.IsOpening || breakpoint.indentationLevel != minimumIndentationLevel)
                {
                    continue; // Keep looking for the opening punctuation at the right level.
                }
                shouldSplitDelimitedItems = breakpoint.ShouldSplitItems;

                // Break before certain opening punctuation. e.g.
                //
                //      someScope { someText moreLongTextHere }.
                //               /\ <---- Yes, break before curly braces.
                //
                //      function(parameterOne, parameterTwo)
                //             /\ <---- No, keep parentheses on same line as call.
                //
                // TODO: Check with {-# #-}
                if (textPosition > 0 &&
                    breakpointOpportunities[(int)textPosition - 1].ShouldBreakAfter &&
                    !IsLineBreakAdjacent(breakpointOpportunities, textPosition, LookDirection.Backward))
                {
                    if (SplitLineRanges(lineRanges, lineIndex, textPosition))
                    {
                        ++lineIndex;
                    }
                }
                // Break after the opening punctuation.
                //
                //      someScope { someText moreLongTextHere }
                //                /\ <---- Break after curly braces to separate statements.
                //
                //      function(parameterOne, parameterTwo)
                //              /\ <---- Break after parentheses to separate parameters.
                //
                // TODO: Check with {-# #-}
                if (!IsLineBreakAdjacent(breakpointOpportunities, textPosition + 1, LookDirection.Forward))
                {
                    SplitLineRanges(lineRanges, lineIndex, textPosition + 1);
                }

                break;
            }

            // Scan forward to split the closing punctuation and potentially split any delimited items within the scope
            // that are at the same nesting level.
            for (int nextLineIndex = lineIndex + 1; nextLineIndex < lineRanges.Count; ++nextLineIndex)
            {
                LineRange nextLineRange = lineRanges[nextLineIndex];
                for (uint textPosition = nextLineRange.start; textPosition < nextLineRange.end; textPosition++)
                {
                    // Break before closing punctuation, unless there's already a line break. e.g.
                    //
                    //      someScope { someText moreLongTextHere }
                    //                                           /\ <---- Break before closing punctuation.
                    var opportunity = breakpointOpportunities[(int)textPosition];
                    if (opportunity.indentationLevel <= minimumIndentationLevel)
                    {
                        if (!IsLineBreakAdjacent(breakpointOpportunities, textPosition, LookDirection.Backward))
                        {
                            SplitLineRanges(lineRanges, nextLineIndex, textPosition);
                        }
                        break; // Exited the nested scope, such as after ] } ) >.
                    }

                    // Split after each delimited item at the same nesting level, not subitems. e.g.
                    //
                    //      config<capabilities = {}, subconfig = {}, alignment = {}, key = value>
                    //                              /\              /\              /\
                    uint delimiterBreakPosition = textPosition + 1;
                    if (
                        shouldSplitDelimitedItems &&
                        opportunity.indentationLevel == minimumIndentationLevel + 1 &&
                        opportunity.CanSplitAfter &&
                        !IsLineBreakAdjacent(breakpointOpportunities, delimiterBreakPosition, LookDirection.Forward)
                        )
                    {
                        SplitLineRanges(lineRanges, nextLineIndex, delimiterBreakPosition);
                    }
                } // for textPosition
            } // for nextLineIndex
        }

        // Concatenate the line ranges together, inserting indentation and additional line breaks as needed.
        public static string GetWrappedText(
            string inputText,
            LineBreakpointOpportunity[] breakpointOpportunities,
            List<LineRange> lineRanges,
            uint lineIndentationPerLevel
            )
        {
            if (string.IsNullOrEmpty(inputText))
            {
                return String.Empty; // Nothing to wrap.
            }

            Debug.Assert(breakpointOpportunities != null);
            Debug.Assert(lineRanges != null);
            Debug.Assert(breakpointOpportunities.Length == inputText.Length);

            var wrappedText = new StringBuilder();

            // Concatenate the lines together, inserting indentation and additional line breaks as needed.
            foreach (var lineRange in lineRanges)
            {
                uint lineIndentationLevel = (lineRange.start < breakpointOpportunities.Length) ? breakpointOpportunities[(int)lineRange.start].indentationLevel : 0u;
                uint indentation = lineIndentationLevel * lineIndentationPerLevel;
                wrappedText.Append(' ', (int)indentation);

                wrappedText.Append(inputText.Substring((int)lineRange.start, (int)lineRange.Length));

                // Add an explicit line break if there isn't already one in the input.
                if (!breakpointOpportunities[lineRange.end - 1].MustBreakAfter)
                {
                    wrappedText.Append("\r\n");
                }
            }

            return wrappedText.ToString();
        }
    }
}
