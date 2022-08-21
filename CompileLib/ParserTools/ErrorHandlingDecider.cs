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
    internal enum ErrorHandlingDecision { Skip, Stop, Before, Instead, FoldAndRaise, NextHandler }

    /// <summary>
    /// Internal class for object providing error handling decisions
    /// </summary>
    internal class ErrorHandlingDecider
    {
        public Token NextToken { get; }
        public object? Argument { get; private set; }
        public Token TokenArgument
        {
            get => (Token)Argument;
            private set => Argument = value;
        }
        public ErrorHandlingDecision Decision { get; private set; } = ErrorHandlingDecision.Stop;

        public ErrorHandlingDecider(Token nextToken)
        {
            NextToken = nextToken;
        }

        public void Skip()
            => Decision = ErrorHandlingDecision.Skip;

        public void Stop()
            => Decision = ErrorHandlingDecision.Stop;

        public void PerformBefore(Token token)
        {
            Decision = ErrorHandlingDecision.Before;
            TokenArgument = token;
        }

        public void PerformInstead(Token token)
        {
            Decision = ErrorHandlingDecision.Instead;
            TokenArgument = token;
        }

        public void FoldAndReraise(object? foldResult)
        {
            Decision = ErrorHandlingDecision.FoldAndRaise;
            Argument = foldResult;
        }

        public void NextHandler()
            => Decision = ErrorHandlingDecision.NextHandler;
    }
}
