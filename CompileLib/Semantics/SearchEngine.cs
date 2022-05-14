using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Common;

namespace CompileLib.Semantics
{
    internal class SearchEngine
    {
        private SortedDictionary<(string, int), SearchFunction> funcs = new();

        public bool AddRule(SearchFunction func)
        {
            var name = func.Name;
            var count = func.Parameters.Length;
            if (funcs.ContainsKey((name, count))) return false;
            funcs.Add((name, count), func);
            func.Body.SetEngine(this);
            return true;
        }

        private static (string, int)[] embeddedFuncs =
        {
            ("name", 1),
            ("relation", 1),
            ("type", 1),
            ("attribute", 1),
            ("attribute", 2),
        };

        public void Check()
        {
            SortedSet<(string, int)> funcNames = new(funcs.Keys.Concat(embeddedFuncs));
            foreach(var f in funcs.Values)
                f.Check(funcNames);

            List<SearchRule> rules = new();
            foreach(var f in funcs.Values)
                f.Body.CollectRules(rules);

            OrderedGraph g = new(rules.Count);
            for (int i = 0; i < rules.Count; i++)
                rules[i].Vertex = i;
            bool[] used = new bool[rules.Count];

            foreach(var r in rules)
            {
                if (r is ChainSearchRule)
                {
                    used[r.Vertex] = true;
                }
                else if (r is SearchRuleUnion u)
                {
                    g.AddEdge(u.Left.Vertex, u.Vertex);
                    g.AddEdge(u.Right.Vertex, u.Vertex);
                }
                else if (r is SearchRuleIntersection i)
                {
                    g.AddEdge(i.Left.Vertex, i.Vertex);
                    g.AddEdge(i.Right.Vertex, i.Vertex);
                }
                else if (r is SearchRuleCall c)
                {
                    var key = (c.Function, c.Args.Length);
                    if (funcs.ContainsKey(key))
                        g.AddEdge(funcs[key].Body.Vertex, c.Vertex);
                }
                else throw new NotImplementedException();
            }

            g.FindReachable(used);
            for (int i = 0; i < rules.Count; i++)
                rules[i].CallsChainRule = used[i];
        }

        public bool FunctionExists(string name, int argsCnt)
        {
            return funcs.ContainsKey((name, argsCnt)) || embeddedFuncs.Contains((name, argsCnt));
        }

        public void Search(CodeObject obj, string func, string[] args, List<SearchResult> result)
        {
            if (funcs.ContainsKey((func, args.Length)))
            {
                var f = funcs[(func, args.Length)];
                SortedDictionary<string, string> var2val = new();
                for (int i = 0; i < args.Length; i++)
                    var2val.Add(f.Parameters[i], args[i]);
                f.Body.Search(obj, var2val, result);
            }
            else if (func == "name" && args.Length == 1)
            {
                obj.GetByName(args[0], result);
            }
            else if (func == "relation" && args.Length == 1)
            {
                obj.GetByRelation(args[0], result);
            }
            else if (func == "type" && args.Length == 1)
            {
                obj.GetByType(args[0], result);
            }
            else if (func == "attribute" && args.Length == 1)
            {
                obj.GetByAttribute(result, args[0]);
            }
            else if (func == "attribute" && args.Length == 2)
            {
                obj.GetByAttribute(result, args[0], args[1]);
            }
            else throw new NotImplementedException();
        }

        public bool Satisfies(SearchResult obj, string func, string[] args)
        {
            if (funcs.ContainsKey((func, args.Length)))
            {
                var f = funcs[(func, args.Length)];
                SortedDictionary<string, string> var2val = new();
                for (int i = 0; i < args.Length; i++)
                    var2val.Add(f.Parameters[i], args[i]);
                return f.Body.Satisfies(obj, var2val);
            }
            else if (func == "name" && args.Length == 1)
            {
                return obj.Result.Name == args[0];
            }
            else if (func == "relation" && args.Length == 1)
            {
                return obj.Relation == args[0];
            }
            else if (func == "type" && args.Length == 1)
            {
                return obj.Result.Type == args[0];
            }
            else if (func == "attribute" && args.Length == 1)
            {
                return obj.Result.HasAttribute(args[0]);
            }
            else if (func == "attribute" && args.Length == 2)
            {
                return obj.Result.GetAttribute(args[0]) == args[1];
            }
            else throw new NotImplementedException();
        }
    }
}
