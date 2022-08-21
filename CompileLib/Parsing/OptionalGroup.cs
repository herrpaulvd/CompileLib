using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Class used in the syntax analysis as a result of Optional attribute sequence folding
    /// </summary>
    internal class OptionalGroup : IGroup
    {
        private readonly object?[] children;
        private readonly int divisor;

        public OptionalGroup(object?[] children, int divisor)
        {
            this.children = children;
            this.divisor = divisor;
        }

        public IEnumerable<object?> Expand()
        {
            if (children.Length == divisor)
                return children;
            return children.Concat(Enumerable.Repeat((object?)null, divisor - children.Length));
        }
    }
}
