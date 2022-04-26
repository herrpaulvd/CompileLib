using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompileLib.Common;

namespace CompileLib.ParserTools
{
    internal enum ErrorHandlingDecision { Skip, Stop, Before, Instead }

    internal class ErrorHandlingDecider
    {
        public Token NextToken { get; }
        public Token Argument { get; private set; }
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
            Argument = token;
        }

        public void PerformInstead(Token token)
        {
            Decision = ErrorHandlingDecision.Instead;
            Argument = token;
        }
    }
}
