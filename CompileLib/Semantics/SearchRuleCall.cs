using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Semantics
{
    internal class SearchRuleCall : SearchRule
    {
        public string Function { get; private set; }
        public string[] Args { get; private set; }
        private SearchEngine engine;

        public SearchRuleCall(int line, int column, string function, string[] args)
            : base(line, column)
        {
            Function = function;
            Args = args;
        }

        public override void Check(SortedSet<(string, int)> funcs, SortedSet<string> args)
        {
            if (funcs.Contains((Function, Args.Length)))
            {
                for (int i = 0; i < Args.Length; i++)
                    if (Args[i][0] == '@' && !args.Contains(Args[i]))
                        throw new SearchLangParsingException($"Argument {Args[i]} does not exist", Line, Column);
            }
            else throw new SearchLangParsingException($"Function {Function} with {Args.Length} arguments does not exist", Line, Column);
        }

        public override void SetEngine(SearchEngine engine)
        {
            this.engine = engine;
        }

        public override void Search(CodeObject obj, SortedDictionary<string, string> var2val, List<SearchResult> result)
        {
            string[] args = new string[Args.Length];
            for (int i = 0; i < Args.Length; i++)
                args[i] = var2val.ContainsKey(Args[i]) ? var2val[Args[i]] : Args[i];
            engine.Search(obj, Function, args, result);
        }

        public override bool Satisfies(SearchResult obj, SortedDictionary<string, string> var2val)
        {
            string[] args = new string[Args.Length];
            for (int i = 0; i < Args.Length; i++)
                args[i] = var2val.ContainsKey(Args[i]) ? var2val[Args[i]] : Args[i];
            return engine.Satisfies(obj, Function, args);
        }

        public override void CollectRules(List<SearchRule> rules)
        {
            rules.Add(this);
        }
    }
}
