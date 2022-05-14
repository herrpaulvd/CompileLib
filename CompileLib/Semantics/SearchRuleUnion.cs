using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Semantics
{
    internal class SearchRuleUnion : SearchRule
    {
        public SearchRule Left { get; private set; }
        public SearchRule Right { get; private set; }

        public SearchRuleUnion(SearchRule left, SearchRule right)
            : base(left.Line, left.Column)
        {
            Left = left;
            Right = right;
        }

        public override void Check(SortedSet<(string, int)> funcs, SortedSet<string> args)
        {
            Left.Check(funcs, args);
            Right.Check(funcs, args);
        }

        public override void Search(CodeObject obj, SortedDictionary<string, string> var2val, List<SearchResult> result)
        {
            Left.Search(obj, var2val, result);
            Right.Search(obj, var2val, result);
        }

        public override bool Satisfies(SearchResult obj, SortedDictionary<string, string> var2val)
        {
            return Left.Satisfies(obj, var2val) || Right.Satisfies(obj, var2val);
        }

        public override void SetEngine(SearchEngine engine)
        {
            Left.SetEngine(engine);
            Right.SetEngine(engine);
        }

        public override void CollectRules(List<SearchRule> rules)
        {
            rules.Add(this);
            Left.CollectRules(rules);
            Right.CollectRules(rules);
        }
    }
}
