using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class Method : CodeObject
    {
        public CodeObject[] Parameters { get; private set; }

        public Method(string name, string type, int line, int column, CodeObject[] parameters)
            : base(name, type, line, column)
        {
            Parameters = parameters;
            foreach(var p in Parameters)
            {
                AddRelation("parameter", p);
            }
        }
    }
}
