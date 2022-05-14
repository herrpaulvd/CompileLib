using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Semantics
{
    internal class SearchRuleIntersection : SearchRule
    {
        public SearchRule Left { get; private set; }
        public SearchRule Right { get; private set; }

        public SearchRuleIntersection(SearchRule left, SearchRule right)
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
            List<SearchResult> prevres = new();
            Left.Search(obj, var2val, prevres);
            if (Right.CallsChainRule)
            {
                List<SearchResult> set = new();
                Right.Search(obj, var2val, set);
                result.AddRange(
                    prevres.Where(
                        res => set.Any(
                            r => ReferenceEquals(r.Result, res.Result)
                            && r.Relation == res.Relation
                            )
                        )
                    );
            }
            else
            {
                result.AddRange(prevres.Where(res => Right.Satisfies(res, var2val)));
            }
        }

        public override bool Satisfies(SearchResult obj, SortedDictionary<string, string> var2val)
        {
            return Left.Satisfies(obj, var2val) && Right.Satisfies(obj, var2val);
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
