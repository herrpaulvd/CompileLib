using CompileLib.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.ParserTools
{
    /// <summary>
    /// Internal error handler interface
    /// </summary>
    internal interface IErrorHandler
    {
        ErrorHandlingDecision Handle(AnyParsed[] prefix, Parsed<string> nextToken);
    }
}
