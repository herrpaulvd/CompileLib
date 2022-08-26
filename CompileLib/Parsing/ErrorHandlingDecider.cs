using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.ParserTools;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Class for sending error handling decisions
    /// </summary>
    public class ErrorHandlingDecider
    {
        /// <summary>
        /// Function to define incoming tokens
        /// </summary>
        private readonly Func<Parsed<string>, int?> DefineTokenType;
        /// <summary>
        /// Result of error handling
        /// </summary>
        internal ErrorHandlingDecision Result { get; private set; } = ErrorHandlingDecision.Stop;
        /// <summary>
        /// The token reading of which has caused an error
        /// </summary>
        public Parsed<string> NextToken { get; }

        internal ErrorHandlingDecider(Parsed<string> nextToken, Func<Parsed<string>, int?> defineTokenType)
        {
            DefineTokenType = defineTokenType;
            NextToken = nextToken;
        }

        /// <summary>
        /// Parsed<string> -> Token convertion
        /// </summary>
        /// <param name="parsed">The arg to convert</param>
        /// <returns></returns>
        /// <exception cref="ParsingException"></exception>
        private Common.Token ParsedToToken(Parsed<string> parsed)
            => new(DefineTokenType(parsed), parsed.Self ?? throw new ParsingException("Null-tokens are not allowed"), parsed.Line, parsed.Column);

        /// <summary>
        /// Say that decision is to skip the token
        /// </summary>
        public void Skip() => Result = ErrorHandlingDecision.Skip;
        /// <summary>
        /// Say that decision is to stop the analysis
        /// </summary>
        public void Stop() => Result = ErrorHandlingDecision.Stop;
        /// <summary>
        /// Say that before the token the analyzer must read another token
        /// </summary>
        /// <param name="token">The token to read</param>
        public void PerformBefore(Parsed<string> token)
            => Result = ErrorHandlingDecision.PerformBefore(ParsedToToken(token));
        /// <summary>
        /// Say that instead of the token the analyzer must read another token
        /// </summary>
        /// <param name="token">The token to read</param>
        public void PerformInstead(Parsed<string> token)
            => Result = ErrorHandlingDecision.PerformInstead(ParsedToToken(token));
        /// <summary>
        /// Say that the production must be folded and the error must be raised in the parent production
        /// </summary>
        /// <param name="foldResult">Result of the folding</param>
        public void FoldAndReraise(object? foldResult)
            => Result = ErrorHandlingDecision.FoldAndReraise(foldResult);
        /// <summary>
        /// Say that this handler refuses to perform the token and allows to perform it with another handler
        /// </summary>
        public void NextHandler() => Result = ErrorHandlingDecision.NextHandler;
    }
}
