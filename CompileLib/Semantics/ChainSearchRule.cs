using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Semantics
{
    internal class ChainSearchRule : SearchRule
    {
        public SearchRule Filter { get; private set; }
        public SearchRule Selector { get; private set; }
        public bool AddFiltered { get; private set; }

        public ChainSearchRule(SearchRule filter, SearchRule selector, bool addFiltered)
            : base(filter.Line, filter.Column)
        {
            Filter = filter;
            Selector = selector;
            AddFiltered = addFiltered;
        }

        public override void Check(SortedSet<(string, int)> funcs, SortedSet<string> args)
        {
            Filter.Check(funcs, args);
            Selector.Check(funcs, args);
        }

        public override void Search(CodeObject obj, SortedDictionary<string, string> var2val, List<SearchResult> result)
        {
            List<SearchResult> filtered = new();
            Filter.Search(obj, var2val, filtered);
            if (AddFiltered)
                result.AddRange(filtered);
            foreach(var o in filtered)
                Selector.Search(o.Result, var2val, result);
        }

        public override bool Satisfies(SearchResult obj, SortedDictionary<string, string> var2val)
        {
            // this method cannot be implemented
            throw new NotImplementedException();
        }

        public override void SetEngine(SearchEngine engine)
        {
            Filter.SetEngine(engine);
            Selector.SetEngine(engine);
        }

        public override void CollectRules(List<SearchRule> rules)
        {
            rules.Add(this);
            Filter.CollectRules(rules);
            Selector.CollectRules(rules);
        }
    }
}
