using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Semantics
{
    public class SemanticNetwork
    {
        private SearchEngine engine;

        public SemanticNetwork(string rulesCode)
        {
            engine = SearchLanguageParser.Instance.Parse(rulesCode);
        }

        public IList<SearchResult> Search(CodeObject obj, string rule, params string[] args)
        {
            if (!engine.FunctionExists(rule, args.Length))
                throw new ArgumentException("Invalid rule name or args count", nameof(rule));
            List<SearchResult> result = new();
            engine.Search(obj, rule, args, result);
            return result;
        }
    }
}
