using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.ParserTools
{
    /// <summary>
    /// Exception throwed when LR(1)-analysis stops
    /// </summary>
    internal class LRStopException : Exception
    {
        public Common.Token Token { get; }

        public LRStopException(Common.Token token) 
            : base($"[Type {token.Type}][String {token.Self}][Line {token.Line}][Column {token.Column}] LR-Analysis has been stopped by Error Handling Decider")
        {
            Token = token;
        }
    }
}
