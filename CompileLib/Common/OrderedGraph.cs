using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Common
{
    internal class OrderedGraph
    {
        private int n;
        private List<int>[] g;
        private List<int>[] rg;

        public OrderedGraph(int n)
        {
            this.n = n;
            g = new List<int>[n];
            rg = new List<int>[n];
        }

        public void AddEdge(int x, int y)
        {
            g[x].Add(y);
            rg[y].Add(x);
        }

        public void GetMinMaxArrays(out int[] min, out int[] max)
        {
            // topological sort
            bool[] used = new bool[n];
            int[] progress = new int[n];
            List<int> order = new();
            Stack<int> dfsStack = new();
            for(int i = 0; i < n; i++)
            {
                if (used[i]) continue;
                dfsStack.Push(i);
                while(dfsStack.Count > 0)
                {
                    int x = dfsStack.Peek();
                    used[x] = true;
                    for(ref int ptr = ref progress[x]; ptr < g[x].Count; ptr++)
                    {
                        int y = g[x][ptr];
                        if(used[y]) continue;
                        dfsStack.Push(y);
                        break;
                    }
                    if(progress[x] == g[x].Count)
                    {
                        dfsStack.Pop();
                        order.Add(x);
                    }
                }
            }
            order.Reverse();

            // components detecting
            var comp = new int[n];
            Array.Fill(used, false);
            int compCount = 0;
            foreach(int i in order)
            {
                if (used[i]) continue;
                dfsStack.Push(i);
                while(dfsStack.Count > 0)
                {
                    int x = dfsStack.Pop();
                    comp[x] = compCount;
                    used[x] = true;
                    foreach(int y in g[x])
                        if(!used[y])
                            dfsStack.Push(y);
                }
                compCount++;
            }

            // condensation of g and rg
            var cg = new List<int>[compCount];
            var crg = new List<int>[compCount];
            for(int x = 0; x < n; x++)
            {
                foreach (int y in g[x])
                    cg[comp[x]].Add(comp[y]);
                foreach(int y in rg[x])
                    crg[comp[x]].Add(comp[y]);
            }

            // building minmax
            int[] BuildAgg(List<int>[] g, List<int>[] cg, Func<int, int, int> aggF, int neutral)
            {
                Array.Fill(used, false);
                Array.Fill(progress, 0);
                int[] cagg = new int[compCount];
                Array.Fill(cagg, neutral);
                for (int x = 0; x < n; x++)
                    cagg[comp[x]] = aggF(cagg[comp[x]], x);

                for(int i = 0; i < compCount; i++)
                {
                    if (used[i]) continue;
                    dfsStack.Push(i);
                    while (dfsStack.Count > 0)
                    {
                        int x = dfsStack.Peek();
                        used[x] = true;
                        for (ref int ptr = ref progress[x]; ptr < cg[x].Count; ptr++)
                        {
                            int y = cg[x][ptr];
                            if (used[y]) continue;
                            dfsStack.Push(y);
                            break;
                        }
                        if (progress[x] == g[x].Count)
                        {
                            dfsStack.Pop();
                            foreach (int y in cg[x])
                                cagg[x] = aggF(cagg[x], cagg[y]);
                        }
                    }
                }

                int[] agg = new int[n];
                for (int x = 0; x < n; x++)
                    agg[x] = cagg[comp[x]];
                return agg;
            }
            min = BuildAgg(g, cg, Math.Min, n);
            max = BuildAgg(rg, crg, Math.Max, -1);
        }
    }
}
