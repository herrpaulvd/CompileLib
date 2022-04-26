using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    public class AnalysisStopException : Exception
    {
        public Token Token { get; }

        public AnalysisStopException(Token token)
            : base($"[Tag {token.Tag}][String {token.Self}][Line {token.Line}][Column {token.Column}] Syntax analysis has been stopped while error handling")
        {
            Token = token;
        }
    }
}
