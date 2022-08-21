using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Class for sending error handling decisions
    /// </summary>
    public class ErrorHandlingDecider
    {
        private readonly ParserTools.ErrorHandlingDecider originalDecider;
        private readonly Func<Token, int> strToType;
        /// <summary>
        /// The token reading of which has caused an error
        /// </summary>
        public Token NextToken { get; }

        internal ErrorHandlingDecider(ParserTools.ErrorHandlingDecider originalDecider, string tokenTypeName, Func<Token, int> strToType)
        {
            NextToken = new Token(tokenTypeName, originalDecider.NextToken.Self, originalDecider.NextToken.Line, originalDecider.NextToken.Column);
            this.originalDecider = originalDecider;
            this.strToType = strToType;
        }
        /// <summary>
        /// Say that decision is to skip the token
        /// </summary>
        public void Skip()
            => originalDecider.Skip();
        /// <summary>
        /// Say that decision is to stop the analysis
        /// </summary>
        public void Stop()
            => originalDecider.Stop();
        /// <summary>
        /// Say that before the token the analyzer must read another token
        /// </summary>
        /// <param name="token">The token to read</param>
        public void PerformBefore(Token token)
            => originalDecider.PerformBefore(new Common.Token(strToType(token), token.Self, token.Line, token.Column));
        /// <summary>
        /// Say that instead of the token the analyzer must read another token
        /// </summary>
        /// <param name="token">The token to read</param>
        public void PerformInstead(Token token)
            => originalDecider.PerformInstead(new Common.Token(strToType(token), token.Self, token.Line, token.Column));
        /// <summary>
        /// Say that the production must be folded and the error must be raised in the parent production
        /// </summary>
        /// <param name="foldResult">Result of the folding</param>
        public void FoldAndReraise(object? foldResult)
            => originalDecider.FoldAndReraise(foldResult);
        /// <summary>
        /// Say that this handler refuses to perform the token and allows to perform it with another handler
        /// </summary>
        public void NextHandler()
            => originalDecider.NextHandler();
    }
}
