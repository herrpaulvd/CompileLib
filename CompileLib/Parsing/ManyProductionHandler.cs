using CompileLib.ParserTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// The handler for subproductions created from Many attribute
    /// </summary>
    internal class ManyProductionHandler : IProductionHandler, IErrorHandler
    {
        private readonly int divisor;

        public ManyProductionHandler(int divisor)
        {
            this.divisor = divisor;
        }

        public object? Handle(object?[] children)
        {
            return new ManyGroup(children, divisor);
        }

        public void Handle(object?[] prefix, ParserTools.ErrorHandlingDecider decider)
        {
            decider.FoldAndReraise(new ManyGroup(prefix, divisor));
        }
    }
}
