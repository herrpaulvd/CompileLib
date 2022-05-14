using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Semantics
{
    internal class SearchFunction
    {
        public string Name { get; private set; }
        public SearchRule Body { get; private set; }
        public string[] Parameters { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

        public SearchFunction(SearchRule body, string name, int line, int column, string[] parameters)
        {
            Body = body;
            Name = name;
            Parameters = parameters;
            Line = line;
            Column = column;
        }

        public void Check(SortedSet<(string, int)> funcs)
        {
            Body.Check(funcs, new(Parameters));
        }
    }
}
