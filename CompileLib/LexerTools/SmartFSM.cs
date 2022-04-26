using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.LexerTools
{
    internal class SmartFSM : IMachine
    {
        private readonly (Predicate<char>, int)[][] transition;
        private readonly bool[] isFinal;
        
        private int revision = 0;
        private readonly int[] used;
        private List<int> states;
        private List<int> nextStates;

        public SmartFSM((Predicate<char>, int)[][] transition, bool[] isFinal)
        {
            this.transition = transition;
            this.isFinal = isFinal;
            used = new int[isFinal.Length];
            states = new(isFinal.Length);
            nextStates = new(isFinal.Length);
        }

        public bool StateIsFinal => states.Any(s => isFinal[s]);

        public void Start()
        {
            states.Clear();
            states.Add(0);
        }

        public bool Tact(char c)
        {
            nextStates.Clear();
            revision++;
            foreach(var s in states)
                foreach(var (p, next) in transition[s])
                    if(used[next] != revision && p(c))
                    {
                        used[next] = revision;
                        nextStates.Add(next);
                    }
            var t = states;
            states = nextStates;
            nextStates = t;
            return states.Count > 0;
        }
    }
}
