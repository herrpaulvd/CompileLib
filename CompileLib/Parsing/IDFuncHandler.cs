using CompileLib.ParserTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Handler for productions like A ::= B
    /// </summary>
    internal class IDFuncHandler : IProductionHandler, IErrorHandler
    {
        public object? Handle(object?[] children)
        {
            return children[0];
        }

        public void Handle(object?[] prefix, ParserTools.ErrorHandlingDecider decider)
        {
            decider.FoldAndReraise(null);
        }

        public static readonly IDFuncHandler Instance = new();
    }
}
