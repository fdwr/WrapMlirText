using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static TextWrapper.TextWrapper;

namespace WrapMlirText
{
    internal static class ProgramCli
    {
        public static uint DefaultMaximumLineLength = 120;
        public static uint DefaultLineIndentationPerLevel = 4;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string inputFileName;
            string outputFileName;
            uint lineLength = DefaultMaximumLineLength;
            uint lineIndentation = DefaultLineIndentationPerLevel;

            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: WrapMlirText <inputTextFile> <outputTextFile> <lineLength> <lineIndent>");
                return;
            }

            inputFileName = args[0];
            outputFileName = args.Length > 1 ? args[1] : "";

            if (args.Length > 2 && (!uint.TryParse(args[2], out lineLength) || lineLength <= 0))
            {
                System.Console.WriteLine($"Invalid maximum line length '{args[2]}'.");
                return;
            }
            if (args.Length > 3 && !uint.TryParse(args[3], out lineIndentation))
            {
                System.Console.WriteLine($"Invalid line indentation per level '{args[3]}'.");
            }
            System.Console.Write($"input filename: {inputFileName}\r\noutput fileName: {outputFileName}\r\nline length: {lineLength}\r\nline indentation: {lineIndentation}\r\n");

            WrapMlirText(inputFileName, outputFileName, lineLength, lineIndentation);
        }

        static void WrapMlirText(string inputFileName, string outputFileName, uint maximumLineLength, uint lineIndentationPerLevel)
        {
            string inputText;
            string outputText;

            try
            {
                inputText = System.IO.File.ReadAllText(inputFileName);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error reading input file '{inputFileName}': {ex.Message}");
                return;
            }

            var lineBreakpointOpportunities = GetLineBreakpointOpportunities(inputText, defaultBreakPairTable, defaultCategoryBreakFlags);
            var lineRanges = GetLineRanges(inputText, lineBreakpointOpportunities, maximumLineLength, lineIndentationPerLevel);
            outputText = GetWrappedText(inputText, lineBreakpointOpportunities, lineRanges, lineIndentationPerLevel);

            int oldLineCount = lineBreakpointOpportunities.Count(o => o.MustBreakAfter);
            int newLineCount = lineRanges.Count;
            System.Console.Write($"old line count: {oldLineCount}\r\nnew line count: {newLineCount}\r\n");

            if (!string.IsNullOrEmpty(outputFileName))
            {
                try
                {
                    System.IO.File.WriteAllText(outputFileName, outputText);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error writing to output file '{outputFileName}': {ex.Message}");
                    return;
                }
            }
            else // Just print the output text to the console if no output file is specified.
            {
                System.Console.Write(outputText);
            }
        }
    }
}
