using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Class used in the syntax analysis as a result of Many attribute sequence folding
    /// </summary>
    internal class ManyGroup : IGroup
    {
        private readonly object?[] children;
        private readonly int divisor;

        public ManyGroup(object?[] children, int divisor)
        {
            this.children = children;
            this.divisor = divisor;
        }

        private void GetLeaves(List<object?> result)
        {
            foreach (var e in children)
                if (e is ManyGroup g)
                    g.GetLeaves(result);
                else
                    result.Add(e);
        }

        public IEnumerable<object?> Expand()
        {
            List<object?> leaves = new();
            GetLeaves(leaves);

            var result = new object?[divisor];
            for (int i = 0; i < divisor; i++)
            {
                int itemLength = leaves.Count / divisor;
                if (i < leaves.Count % divisor) itemLength++;
                result[i] = new UnknownArray(itemLength);
            }
            for (int i = 0; i < leaves.Count; i++)
                (result[i % divisor] as UnknownArray)[i / divisor] = leaves[i];
            return result;
        }
    }
}
