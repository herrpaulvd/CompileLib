using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using CompileLib.Parsing;

namespace CompileLib.ParserTools
{
    /// <summary>
    /// LR(1) builder
    /// </summary>
    internal class GrammarBuilder
    {
        /// <summary>
        /// Internal GrammarBuilder's representation of productions
        /// </summary>
        private struct Production
        {
            public int Start;
            public int[] Body;
            public IProductionHandler ProductionHandler;
            public IErrorHandler? ErrorHandler;

            public Production(int start, int[] body, IProductionHandler productionHandler, IErrorHandler? errorHandler)
            {
                Start = start;
                Body = body;
                ProductionHandler = productionHandler;
                ErrorHandler = errorHandler;
            }
        }

        /// <summary>
        /// Default error handler stopping the analysis
        /// </summary>
        private class DefaultErrorHandler : IErrorHandler
        {
            public ErrorHandlingDecision Handle(AnyParsed[] prefix, Parsed<string> nextToken)
            {
                return ErrorHandlingDecision.Stop;
            }
            public static readonly DefaultErrorHandler Instance = new();
        }

        private readonly List<Production> productions = new();
        private readonly List<SortedSet<int>?> foldingBansByFirst = new();
        private readonly List<SortedSet<int>?> foldingBans = new();
        private readonly List<SortedSet<int>?> carryBans = new();
        private readonly List<int>[] prodByStart;
        private readonly int tokensCount;
        private readonly int nonTokensCount;
        private readonly int fictStart;
        private readonly int fictEOF;
        private readonly int mainProduction;
        private const int reservedProductions = 1;

        public IEnumerable<int> GetBodyByProductionIndex(int index)
            => productions[index].Body;

        public void AddProduction(int start, int[] body, IProductionHandler productionHandler, IErrorHandler? errorHandler)
        {
            Debug.Assert(
                start >= 0
                && start < nonTokensCount
                && body.All(c => (c < 0) ? ((~c) < tokensCount) : (c < nonTokensCount)));

            int prodID = productions.Count;
            productions.Add(new(start, body, productionHandler, errorHandler));
            foldingBansByFirst.Add(null);
            foldingBans.Add(null);
            carryBans.Add(null);
            prodByStart[start].Add(prodID);
        }

        private bool FoldingBanned(int p, int c)
            => foldingBans[p] is not null && foldingBans[p].Contains(c);

        private bool CarryBanned(int p, int c)
            => carryBans[p] is not null && carryBans[p].Contains(c);

        /// <summary>
        /// For target production, it bans all foldings when the next token is a character from FIRST(firstFuncSource).
        /// NB! The method must not be called unless all productions are added
        /// </summary>
        /// <param name="targetProduction"></param>
        /// <param name="firstFuncSource"></param>
        public void AddBanFoldingWhenFirstRule(int targetProduction, int firstFuncSource)
        {
            targetProduction += reservedProductions;
            firstFuncSource += reservedProductions;
            (foldingBansByFirst[targetProduction] ??= new()).Add(firstFuncSource);
        }

        /// <summary>
        /// For target production, it bans any folding when the next token is the given character.
        /// </summary>
        /// <param name="targetProduction"></param>
        /// <param name="character"></param>
        public void AddBanFoldingWhenCharacterRule(int targetProduction, int character)
        {
            targetProduction += reservedProductions;
            (foldingBans[targetProduction] ??= new()).Add(character);
        }

        /// <summary>
        /// For target production, it bans any carry when the next token is the given character.
        /// </summary>
        /// <param name="targetProduction"></param>
        /// <param name="character"></param>
        public void AddBanCarryWhenCharacterRule(int targetProduction, int character)
        {
            targetProduction += reservedProductions;
            (carryBans[targetProduction] ??= new()).Add(character);
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
            mainProduction = productions.Count;
            AddProduction(fictStart, new int[] {start}, IDFuncHandler.Instance, null);
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
                            if (FoldingBanned(p, b))
                                continue;
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

            public GotoResult(int tokensCount, int nonTokensCount)
            {
                tokensResult = new List<(int, int, int)>[tokensCount];
                tokensOwners = new int[tokensCount];
                nonTokensResult = new List<(int, int, int)>[nonTokensCount];
                nonTokensOwners = new int[nonTokensCount];
            }

            private static void Update(List<(int, int, int)>[] result, int[] owners, int x, int prod, int pos, int c)
            {
                if(result[x] is null)
                {
                    result[x] = new List<(int, int, int)>();
                    owners[x] = prod;
                }
                else
                {
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
            GotoResult result = new(tokensCount, nonTokensCount);
            SortedSet<int> bannedTransitions = new();
            foreach(var (prod, pos, a) in list)
            {
                var production = productions[prod];
                var body = production.Body;
                if (pos == body.Length && CarryBanned(prod, a))
                    bannedTransitions.Add(a);
            }
            foreach(var (prod, pos, a) in list)
            {
                var production = productions[prod];
                var body = production.Body;
                if (pos < body.Length && !bannedTransitions.Contains(body[pos]))
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
                return next[i] ??= new Trie();
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

        public LRMachine CreateMachine(Func<int?, string> tokenTypeToStr, Func<int?, string> nonTokenTypeToStr)
        {
            RecalcFirstAndEmpty();
            for (int i = 0; i < foldingBansByFirst.Count; i++)
                if (foldingBansByFirst[i] is not null)
                    (foldingBans[i] ??= new()).UnionWith(foldingBansByFirst[i].SelectMany(firstFuncSource => First(productions[firstFuncSource].Body)));

            List<List<(int, int, int)>> lists = new();
            List<(int, int)> parents = new();
            List<LRAction[]> actions = new();
            List<int[]> @goto = new();
            List<List<(int, IErrorHandler, int)>> errorHandlers = new();
            Trie trie = new();

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

                List<(int, IErrorHandler, int)> currHandlers = new();

                foreach (var (prod, pos, c) in lists[i])
                {
                    var start = productions[prod].Start;
                    var body = productions[prod].Body;
                    if (pos == body.Length)
                    {
                        var handler = productions[prod].ProductionHandler;
                        if (prod == mainProduction && c == fictEOF)
                        {
                            if (currActions[~c].IsError)
                            {
                                currActions[~c] = LRAction.AcceptAction;
                                currOwners[~c] = prod;
                            }
                            else
                            {
                                throw new LRConflictException(
                                    new(currActions[~c].Type == LRActionType.Carry, currOwners[~c] - reservedProductions),
                                    new(false, null),
                                    null,
                                    getWay(i));
                            }
                        }
                        else
                        {
                            if (currActions[~c].IsError)
                            {
                                currActions[~c] = LRAction.CreateFold(body.Length, start, handler);
                                currOwners[~c] = prod;
                            }
                            else
                            {
                                throw new LRConflictException(
                                    new(currActions[~c].Type == LRActionType.Carry, currOwners[~c] - reservedProductions),
                                    new(false, prod - mainProduction - 1),
                                    ~c,
                                    getWay(i));
                            }
                        }
                    }
                    else
                    {
                        var errorHandler = productions[prod].ErrorHandler;
                        if (errorHandler is not null)
                        {
                            currHandlers.Add((pos, errorHandler, productions[prod].Start));
                        }
                    }
                }

                actions.Add(currActions);
                @goto.Add(currGoto);
                currHandlers.Add((0, DefaultErrorHandler.Instance, -1));
                errorHandlers.Add(currHandlers);
            }

            return new LRMachine(actions.ToArray(), @goto.ToArray(), errorHandlers.ToArray(), new Common.Token(~fictEOF, "", -1, -1), tokenTypeToStr, nonTokenTypeToStr);
        }
    }
}
