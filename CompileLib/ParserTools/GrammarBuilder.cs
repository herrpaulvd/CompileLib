using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CompileLib.ParserTools
{
    internal class GrammarBuilder
    {
        private struct Production
        {
            public int Start;
            public int[] Body;
            public int PriorityMain;
            public int PriorityNormal;
            public int PriorityError;
            public IProductionHandler ProductionHandler;
            public IErrorHandler? ErrorHandler;

            public Production(int start, int[] body, int priorityMain, int priorityNormal, int priorityError, IProductionHandler productionHandler, IErrorHandler? errorHandler)
            {
                Start = start;
                Body = body;
                PriorityMain = priorityMain;
                PriorityNormal = priorityNormal;
                PriorityError = priorityError;
                ProductionHandler = productionHandler;
                ErrorHandler = errorHandler;
            }
        }

        private class MainHandler : IProductionHandler
        {
            public object? Handle(object?[] children)
            {
                return children[0];
            }
            public static readonly MainHandler Instance = new();
        }

        private class DefaultErrorHandler : IErrorHandler
        {
            public void Handle(object?[] prefix, ErrorHandlingDecider decider)
            {
                decider.Stop();
            }
            public static readonly DefaultErrorHandler Instance = new();
        }

        private readonly List<Production> productions = new();
        private readonly List<int>[] prodByStart;
        private readonly int tokensCount;
        private readonly int nonTokensCount;
        private readonly int fictStart;
        private readonly int fictEOF;
        private readonly int mainProduction;

        public IEnumerable<int> GetBodyByProductionIndex(int index)
            => productions[index].Body;

        public void AddProduction(int start, int[] body, int pMain, int pNormal, int pError, IProductionHandler productionHandler, IErrorHandler? errorHandler)
        {
            Debug.Assert(
                start >= 0
                && start < nonTokensCount
                && body.All(c => (c < 0) ? ((~c) < tokensCount) : (c < nonTokensCount)));
            productions.Add(new(start, body, pMain, pNormal, pError, productionHandler, errorHandler));
            prodByStart[start].Add(productions.Count - 1);
        }

        public GrammarBuilder(int tokensCount, int nonTokensCount, int start)
        {
            this.tokensCount = tokensCount + 1;
            this.nonTokensCount = nonTokensCount + 1;
            fictEOF = ~tokensCount;
            fictStart = nonTokensCount;
            prodByStart = new List<int>[this.nonTokensCount];
            for (int i = 0; i < this.nonTokensCount; i++)
                prodByStart[i] = new();
            AddProduction(fictStart, new int[] {start}, 0, 0, 0, MainHandler.Instance, null);
            mainProduction = productions.Count - 1;
        }

        private bool[] empty;
        private int[][] first;

        private void RecalcFirstAndEmpty()
        {
            // step 1: empties defining
            empty = new bool[nonTokensCount];
            var include = new int[productions.Count];
            
            var q = new Queue<int>();
            var prodByNext = new List<int>[nonTokensCount];
            for(int i = 0; i < nonTokensCount; i++)
                prodByNext[i] = new();

            for(int p = 0; p < productions.Count; p++)
            {
                if(productions[p].Body.Length == 0)
                {
                    int start = productions[p].Start;
                    if(!empty[start])
                    {
                        empty[start] = true;
                        q.Enqueue(start);
                    }
                }
                else if(productions[p].Body[0] >= 0)
                {
                    prodByNext[productions[p].Body[0]].Add(p);
                }
            }

            while(q.Count > 0)
            {
                int nt = q.Dequeue();
                foreach(var p in prodByNext[nt])
                {
                    var body = productions[p].Body;
                    while (include[p] < body.Length && body[include[p]] >= 0 && empty[body[include[p]]])
                        include[p]++;
                    if (include[p] == body.Length)
                    {
                        int start = productions[p].Start;
                        if(!empty[start])
                        {
                            empty[start] = true;
                            q.Enqueue(start);
                        }
                    }
                    else if (include[p] < body.Length && body[include[p]] >= 0)
                    {
                        prodByNext[body[include[p]]].Add(p);
                    }
                }
            }

            // base preparations for the "first" function
            var baseFirst = new List<int>[nonTokensCount];
            for(int i = 0; i < nonTokensCount; i++)
                baseFirst[i] = new();

            for(int p = 0; p < productions.Count; p++)
            {
                var body = productions[p].Body;
                if(include[p] < body.Length)
                {
                    if (body[include[p]] >= 0)
                        include[p]++;
                    else
                        baseFirst[productions[p].Start].Add(body[include[p]]);
                }

                if (include[p] < body.Length && body[include[p]] >= 0)
                    include[p]++;
            }

            // step 2: condensation graph
            var g = new List<int>[nonTokensCount];
            var rg = new List<int>[nonTokensCount];
            for(int i = 0; i < nonTokensCount; i++)
            {
                g[i] = new();
                rg[i] = new();
            }
            for(int p = 0; p < productions.Count; p++)
            {
                var start = productions[p].Start;
                foreach (var end in productions[p].Body.Take(include[p]))
                {
                    g[start].Add(end);
                    rg[end].Add(start);
                }
            }

            // 2.1: topsort
            var used = new bool[nonTokensCount];
            var dfsStack = new Stack<(int, int)>();
            var order = new List<int>();
            for(int i = 0; i < nonTokensCount; i++)
            {
                if(!used[i])
                {
                    used[i] = true;
                    dfsStack.Push((i, 0));
                    do
                    {
                        var (v, cnt) = dfsStack.Pop();
                        if(cnt < g[v].Count)
                        {
                            dfsStack.Push((v, cnt + 1));
                            int u = g[v][cnt];
                            if(!used[u])
                            {
                                used[u] = true;
                                dfsStack.Push((u, 0));
                            }
                        }
                        else
                        {
                            order.Add(v);
                        }
                    } while(dfsStack.Count > 0);
                }
            }

            // 2.2: components
            order.Reverse();
            Array.Fill(used, false);
            var components = new List<List<int>>();
            foreach(int i in order)
            {
                if (!used[i])
                {
                    used[i] = true;
                    dfsStack.Push((i, 0));
                    var comp = new List<int>();
                    components.Add(comp);
                    do
                    {
                        var (v, cnt) = dfsStack.Pop();
                        if (cnt < rg[v].Count)
                        {
                            dfsStack.Push((i, cnt + 1));
                            int u = rg[v][cnt];
                            if (!used[u])
                            {
                                used[u] = true;
                                dfsStack.Push((u, 0));
                            }
                        }
                        else
                        {
                            comp.Add(v);
                        }
                    } while (dfsStack.Count > 0);
                }
            }

            // step 3: first building
            first = new int[nonTokensCount][];
            components.Reverse();
            foreach(var comp in components)
            {
                var result = new SortedSet<int>();
                foreach(int i in comp)
                {
                    result.UnionWith(baseFirst[i]);
                    foreach (int j in g[i])
                        if (first[j] is not null)
                            result.UnionWith(first[j]);
                }
                var resultArray = result.ToArray();
                foreach (int i in comp)
                    first[i] = resultArray;
            }
        }

        private bool SingleFirst(int c, SortedSet<int> result)
        {
            if (c < 0)
            {
                result.Add(c);
                return false;
            }
            else
            {
                result.UnionWith(first[c]);
                return empty[c];
            }
        }

        private SortedSet<int> First(IEnumerable<int> s)
        {
            var result = new SortedSet<int>();
            foreach(var c in s)
                if (!SingleFirst(c, result))
                    return result;
            result.Add(fictEOF);
            return result;
        }

        private List<(int, int, int)> Closure(List<(int, int, int)> list)
        {
            var used = new SortedSet<(int, int, int)>(list);
            var result = new List<(int, int, int)>(list);
            for(int i = 0; i < result.Count; i++)
            {
                var (prod, pos, a) = result[i];
                var body = productions[prod].Body;
                if(pos < body.Length && body[pos] >= 0)
                    foreach(var b in First(body.Skip(pos + 1).Append(a)))
                        foreach(var p in prodByStart[body[pos]])
                        {
                            var triple = (p, 0, b);
                            if(used.Add(triple))
                                result.Add(triple);
                        }
            }
            return result;
        }

        private struct GotoResult
        {
            private readonly List<(int, int, int)>[] tokensResult;
            private readonly List<(int, int, int)>[] nonTokensResult;
            private readonly int[] tokensOwners;
            private readonly int[] nonTokensOwners;
            private readonly Func<int, (int, int)> productionToPriority;

            public GotoResult(int tokensCount, int nonTokensCount, Func<int, (int, int)> productionToPriority)
            {
                tokensResult = new List<(int, int, int)>[tokensCount];
                tokensOwners = new int[tokensCount];
                nonTokensResult = new List<(int, int, int)>[nonTokensCount];
                nonTokensOwners = new int[nonTokensCount];
                this.productionToPriority = productionToPriority;
            }

            private void Update(List<(int, int, int)>[] result, int[] owners, int x, int prod, int pos, int c)
            {
                var p = productionToPriority(prod);
                if(result[x] is null)
                {
                    result[x] = new List<(int, int, int)>();
                    owners[x] = prod;
                }
                else
                {
                    var oldPriority = productionToPriority(owners[x]);
                    if (oldPriority.CompareTo(p) < 0)
                        owners[x] = prod;
                }
                result[x].Add((prod, pos, c));
            }

            public void Add(int x, int prod, int pos, int c)
            {
                if (x < 0)
                    Update(tokensResult, tokensOwners, ~x, prod, pos, c);
                else
                    Update(nonTokensResult, nonTokensOwners, x, prod, pos, c);
            }

            public void Map(Func<List<(int, int, int)>, List<(int, int, int)>> closure)
            {
                for(int i = 0; i < tokensResult.Length; i++)
                    if(tokensResult[i] is not null)
                        tokensResult[i] = closure(tokensResult[i]);

                for(int i = 0; i < nonTokensResult.Length; i++)
                    if(nonTokensResult[i] is not null)
                        nonTokensResult[i] = closure(nonTokensResult[i]);
            }

            // (x, list[.Count > 0], owner)
            private static IEnumerable<(int, List<(int, int, int)>, int)> Extract(List<(int, int, int)>[] result, int[] owners)
            {
                return result.Select((l, i) => (i, l, owners[i])).Where(tuple => tuple.l is not null);
            }
            
            public IEnumerable<(int, List<(int, int, int)>, int)> ExtractTokensResult()
            {
                return Extract(tokensResult, tokensOwners);
            }

            public IEnumerable<(int, List<(int, int, int)>, int)> ExtractNonTokensResult()
            {
                return Extract(nonTokensResult, nonTokensOwners);
            }
        }

        private GotoResult Goto(List<(int, int, int)> list)
        {
            GotoResult result = new(tokensCount, nonTokensCount, p => (productions[p].PriorityMain, productions[p].PriorityNormal));
            foreach(var (prod, pos, a) in list)
            {
                var production = productions[prod];
                var body = production.Body;
                if (pos < body.Length)
                    result.Add(body[pos], prod, pos + 1, a);
            }
            result.Map(Closure);
            return result;
        }

        private static string ListToString(List<(int, int, int)> list)
        {
            list.Sort();
            StringBuilder result = new();
            foreach (var (prod, pos, c) in list)
            {
                result.Append(prod);
                result.Append(' ');
                result.Append(pos);
                result.Append(' ');
                result.Append(~c);
                result.Append(' ');
            }
            return result.ToString();
        }

        private class Trie
        {
            private readonly Trie[] next = new Trie[11];
            private int index = -1;

            private Trie GetOrCreate(char c)
            {
                int i = 10;
                if ('0' <= c && c <= '9')
                    i = c - '0';
                return next[i] ?? (next[i] = new Trie());
            }

            public int AddOrFind(string s, int newIndex)
            {
                Debug.Assert(newIndex >= 0);
                var curr = this;
                foreach(var c in s)
                    curr = curr.GetOrCreate(c);

                if(curr.index == -1)
                    curr.index = newIndex;
                return curr.index;
            }
        }

        private bool SetCheckingPriority(LRAction[] array, int[] owners, int index, LRAction value, int owner)
        {
            if(array[index].IsError)
            {
                owners[index] = owner;
                array[index] = value;
                return true;
            }

            var oldPriority = (productions[owners[index]].PriorityMain, productions[owners[index]].PriorityNormal);
            var newPriority = (productions[owner].PriorityMain, productions[owner].PriorityNormal);
            var compareResult = oldPriority.CompareTo(newPriority);

            if(compareResult < 0)
            {
                owners[index] = owner;
                array[index] = value;
                return true;
            }
            else
            {
                return compareResult > 0;
            }
        }

        public LRMachine CreateMachine()
        {
            RecalcFirstAndEmpty();
            List<List<(int, int, int)>> lists = new();
            List<(int, int)> parents = new();
            List<LRAction[]> actions = new();
            List<int[]> @goto = new();
            List<(int, IErrorHandler)> errorHandlers = new();
            Trie trie = new();

            // TODO: rewrite приоритеты на слайсы
            // как-то посчитать closure по терминалам
            // buffered DFS???

            // TODO: class Graph, в котором можно будет сделать GetWay, FindLoop
            // замена меток рёбер\
            // передать граф в LR??? матричный???

            int[] getWay(int list)
            {
                List<int> result = new();
                while(list != 0)
                {
                    var (parentList, c) = parents[list];
                    result.Add(c);
                    list = parentList;
                }
                result.Reverse();
                return result.ToArray();
            }

            var basePunkt = (mainProduction, 0, fictEOF);
            var firstList = Closure(new List<(int, int, int)> { basePunkt });
            lists.Add(firstList);
            parents.Add((0, 0));
            int maxIndex = trie.AddOrFind(ListToString(firstList), 0);

            for (int i = 0; i <= maxIndex; i++)
            {
                var currActions = new LRAction[tokensCount];
                var currGoto = new int[nonTokensCount];
                var currOwners = new int[tokensCount];

                var gotoResult = Goto(lists[i]);
                foreach (var (t, list, p) in gotoResult.ExtractTokensResult())
                {
                    int index = trie.AddOrFind(ListToString(list), maxIndex + 1);
                    if (index > maxIndex)
                    {
                        maxIndex = index;
                        lists.Add(list);
                        parents.Add((i, ~t));
                    }
                    currActions[t] = LRAction.CreateCarry(index);
                    currOwners[t] = p;
                }

                foreach (var (nt, list, _) in gotoResult.ExtractNonTokensResult())
                {
                    int index = trie.AddOrFind(ListToString(list), maxIndex + 1);
                    if (index > maxIndex)
                    {
                        maxIndex = index;
                        lists.Add(list);
                        parents.Add((i, nt));
                    }
                    currGoto[nt] = index;
                }

                var maxErrorPriority = (int.MinValue, int.MinValue);
                var maxCount = 0;
                IErrorHandler maxErrorHandler = DefaultErrorHandler.Instance;
                foreach (var (prod, pos, c) in lists[i])
                {
                    var start = productions[prod].Start;
                    var body = productions[prod].Body;
                    if (pos == body.Length)
                    {
                        var handler = productions[prod].ProductionHandler;
                        if (prod == mainProduction && c == fictEOF)
                        {
                            if(!SetCheckingPriority(currActions, currOwners, ~c, LRAction.AcceptAction, prod))
                            {
                                throw new LRConflictException(
                                    new(currActions[~c].Type == LRActionType.Carry, currOwners[~c] - mainProduction - 1),
                                    new(false, null),
                                    null,
                                    getWay(i));
                            }
                        }
                        else
                        {
                            if(!SetCheckingPriority(currActions, currOwners, ~c, LRAction.CreateFold(body.Length, start, handler), prod))
                            {
                                throw new LRConflictException(
                                    new(currActions[~c].Type == LRActionType.Carry, currOwners[~c] - mainProduction - 1),
                                    new(false, prod - mainProduction - 1),
                                    ~c,
                                    getWay(i));
                            }
                        }
                    }
                    else
                    {
                        var errorHandler = productions[prod].ErrorHandler;
                        if (errorHandler is null)
                            continue;
                        var errorPriority = (productions[prod].PriorityMain, productions[prod].PriorityError);
                        if(errorPriority.CompareTo(maxErrorPriority) > 0)
                        {
                            maxErrorPriority = errorPriority;
                            maxCount = pos;
                            maxErrorHandler = errorHandler;
                        }
                    }
                }

                actions.Add(currActions);
                @goto.Add(currGoto);
                errorHandlers.Add((maxCount, maxErrorHandler));
            }
            return new LRMachine(actions.ToArray(), @goto.ToArray(), errorHandlers.ToArray(), new Common.Token(~fictEOF, "", -1, -1));
        }
    }
}
