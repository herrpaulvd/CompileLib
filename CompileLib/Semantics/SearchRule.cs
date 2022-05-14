using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Semantics
{
    internal abstract class SearchRule
    {
        public int Line { get; private set; }
        public int Column { get; private set; }

        public SearchRule(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public SearchRule ChangePos(int line, int column)
        {
            Line = line;
            Column = column;
            return this;
        }

        public abstract void Check(SortedSet<(string, int)> funcs, SortedSet<string> args);

        public abstract void SetEngine(SearchEngine engine);
        public abstract void Search(CodeObject obj, SortedDictionary<string, string> var2val, List<SearchResult> result);
        public abstract bool Satisfies(SearchResult obj, SortedDictionary<string, string> var2val);

        public bool CallsChainRule { get; set; }
        public int Vertex { get; set; }
        public abstract void CollectRules(List<SearchRule> rules);
    }
}
