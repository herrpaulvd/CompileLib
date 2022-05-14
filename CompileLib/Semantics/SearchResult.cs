using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Semantics
{
    public struct SearchResult
    {
        public CodeObject Result;
        public string Relation;

        public SearchResult(CodeObject result, string relation)
        {
            Result = result;
            Relation = relation;
        }
    }
}
