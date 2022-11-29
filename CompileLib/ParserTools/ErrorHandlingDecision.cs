using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompileLib.Common;

namespace CompileLib.ParserTools
{
    /// <summary>
    /// Error handling decisions list
    /// </summary>
    internal enum ErrorHandlingDecisionType { Skip, Stop, Before, Instead, FoldAndRaise, NextHandler }

    /// <summary>
    /// Internal representation of CompileLib.Parsing.ErrorHandlingDecider decisions
    /// </summary>
    internal class ErrorHandlingDecision
    {
        /// <summary>
        /// An argument of a decision
        /// </summary>
        public object? Argument { get; private set; }
        /// <summary>
        /// The argument but typeof Token
        /// </summary>
        public Token TokenArgument
        {
            get => (Token)Argument;
            private set => Argument = value;
        }
        /// <summary>
        /// The decision type itself
        /// </summary>
        public ErrorHandlingDecisionType Decision { get; private set; } = ErrorHandlingDecisionType.Stop;

        private ErrorHandlingDecision(ErrorHandlingDecisionType type, object? argument = null)
        {
            Decision = type;
            Argument = argument;
        }

        // for more information, see CompileLib.Parsing.ErrorHandlingDecider
        public static readonly ErrorHandlingDecision Skip = new(ErrorHandlingDecisionType.Skip);
        public static readonly ErrorHandlingDecision Stop = new(ErrorHandlingDecisionType.Stop);
        public static ErrorHandlingDecision PerformBefore(Token token) => new(ErrorHandlingDecisionType.Before, token);
        public static ErrorHandlingDecision PerformInstead(Token token) => new(ErrorHandlingDecisionType.Instead, token);
        public static ErrorHandlingDecision FoldAndReraise(object? foldResult) => new(ErrorHandlingDecisionType.FoldAndRaise, foldResult);
        public static readonly ErrorHandlingDecision NextHandler = new(ErrorHandlingDecisionType.NextHandler);
    }
}
