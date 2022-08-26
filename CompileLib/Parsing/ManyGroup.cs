using CompileLib.ParserTools;
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
        private readonly AnyParsed[] children;
        private readonly int divisor;

        public ManyGroup(AnyParsed[] children, int divisor)
        {
            this.children = children;
            this.divisor = divisor;
        }

        private void GetLeaves(List<AnyParsed> result)
        {
            foreach (var e in children)
                if (e.Self is ManyGroup g)
                    g.GetLeaves(result);
                else
                    result.Add(e);
        }

        public IEnumerable<AnyParsed> Expand()
        {
            List<AnyParsed> leaves = new();
            GetLeaves(leaves);

            var result = new AnyParsed[divisor];
            for (int i = 0; i < divisor; i++)
            {
                int itemLength = leaves.Count / divisor;
                if (i < leaves.Count % divisor) itemLength++;
                result[i] = new(SpecialTags.TAG_UNDEFINED, new UnknownArray(itemLength), -1, -1);
            }
            for (int i = 0; i < leaves.Count; i++)
                (result[i % divisor].Self as UnknownArray)[i / divisor] = leaves[i];
            return result;
        }
    }
}
