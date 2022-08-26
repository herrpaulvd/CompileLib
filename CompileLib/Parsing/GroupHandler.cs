using CompileLib.Common;
using CompileLib.ParserTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    internal class GroupHandler<T> : IProductionHandler, IErrorHandler
        where T : IGroup
    {
        private readonly int divisor;

        public GroupHandler(int divisor)
        {
            this.divisor = divisor;
        }

        public object? Handle(AnyParsed[] children, ref string tag)
        {
            return Activator.CreateInstance(typeof(T), children, divisor);
        }

        public ErrorHandlingDecision Handle(AnyParsed[] prefix, Parsed<string> nextToken)
        {
            return ErrorHandlingDecision.FoldAndReraise(Activator.CreateInstance(typeof(T), prefix, divisor));
        }
    }
}
