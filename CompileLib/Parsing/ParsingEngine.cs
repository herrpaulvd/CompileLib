using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Class for running parsers
    /// </summary>
    public class ParsingEngine
    {
        private readonly LexerTools.Lexer lexer;
        private readonly ParserTools.LRMachine parser;
        private readonly Func<Common.Token, bool> tokenFilter;
        private readonly Func<int, string> typeToStr;

        internal ParsingEngine(LexerTools.Lexer lexer, ParserTools.LRMachine parser, Func<Common.Token, bool> tokenFilter, Func<int, string> typeToStr)
        {
            this.lexer = lexer;
            this.parser = parser;
            this.tokenFilter = tokenFilter;
            this.typeToStr = typeToStr;
        }

        /// <summary>
        /// Method to parse the given char sequence
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream">the input</param>
        /// <returns></returns>
        /// <exception cref="AnalysisStopException"></exception>
        public T? Parse<T>(IEnumerable<char> stream)
        {
            try
            {
                return (T?)parser.Analyze(lexer.GetTokens(stream).Where(tokenFilter));
            }
            catch(ParserTools.LRStopException e)
            {
                throw new AnalysisStopException(new Token(typeToStr(e.Token.Type), e.Token.Self, e.Token.Line, e.Token.Column));
            }
        }

        private static IEnumerable<char> FileStreamToEnumerable(StreamReader stream)
        {
            while (!stream.EndOfStream)
                yield return (char)stream.Read();
        }

        /// <summary>
        /// Method to parse the given file
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName">the input file name</param>
        /// <returns></returns>
        public T? ParseFile<T>(string fileName)
        {
            using var stream = new StreamReader(fileName);
            return Parse<T>(FileStreamToEnumerable(stream));
        }
    }
}
