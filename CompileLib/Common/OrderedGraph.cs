using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CompileLib.Common
{
    /// <summary>
    /// Ordered graph class with some useful operations
    /// </summary>
    internal class OrderedGraph
    {
        private int n;
        private List<int>[] g;

        public OrderedGraph(int n)
        {
            this.n = n;
            g = new List<int>[n];
            for(int i = 0; i < n; i++)
                g[i] = new();
        }

        public void AddEdge(int x, int y)
        {
            g[x].Add(y);
        }

        /// <summary>
        /// The method solves two problems for stack optimisation.
        /// It is given a code execution graph (V = instructions, E = jumps and linear code sequence).
        /// For each instruction, the algorithm finds two values:
        /// 1) the instruction with the minimum number that is reachable from the given one;
        /// 2) the instruction with the maximum number from that the given one is reachable.
        /// </summary>
        /// <param name="min">the answer for the 1st problem</param>
        /// <param name="max">the answer for the 2nd problem</param>
        public void GetMinMaxArrays(out int[] min, out int[] max)
        {
            List<int>[] rg = new List<int>[n];
            for (int i = 0; i < n; i++) rg[i] = new();
            for (int i = 0; i < n; i++) foreach(int j in g[i]) rg[j].Add(i);

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
            for(int i = 0; i < compCount; i++)
            {
                cg[i] = new();
                crg[i] = new();
            }

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
                        if (progress[x] == cg[x].Count)
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

        /// <summary>
        /// Just BFS searching for all reachable vertices from the given ones
        /// </summary>
        /// <param name="used">the given starts being transformed to the answer</param>
        public void FindReachable(bool[] used)
        {
            Queue<int> q = new();
            for(int i = 0; i < n; i++)
                if(used[i])
                    q.Enqueue(i);

            while(q.Count > 0)
            {
                int x = q.Dequeue();
                foreach(int y in g[x])
                    if(!used[y])
                    {
                        used[y] = true;
                        q.Enqueue(y);
                    }
            }
        }
    }
}
