using CompileLib.ParserTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// The handler for subproductions created from Optional attribute
    /// </summary>
    internal class OptionalProductionHandler : IProductionHandler, IErrorHandler
    {
        private readonly int divisor;

        public OptionalProductionHandler(int divisor)
        {
            this.divisor = divisor;
        }

        public object? Handle(object?[] children)
        {
            return new OptionalGroup(children, divisor);
        }

        public void Handle(object?[] prefix, ParserTools.ErrorHandlingDecider decider)
        {
            decider.FoldAndReraise(new OptionalGroup(prefix, divisor));
        }
    }
}
