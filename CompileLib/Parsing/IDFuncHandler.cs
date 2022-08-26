using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompileLib.ParserTools;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Handler for productions like A ::= B
    /// </summary>
    internal class IDFuncHandler : IProductionHandler, IErrorHandler
    {
        public object? Handle(AnyParsed[] children, ref string tag)
        {
            tag = children[0].Tag;
            return children[0].Self;
        }

        public ErrorHandlingDecision Handle(AnyParsed[] prefix, Parsed<string> nextToken)
        {
            return ErrorHandlingDecision.FoldAndReraise(null);
        }

        public static readonly IDFuncHandler Instance = new();
    }
}
