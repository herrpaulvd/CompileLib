using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class Parameter : CodeObject
    {
        public Expression TypeExpression { get; private set; }

        public Parameter(string name, int line, int column, Expression typeExpression) 
            : base(name, "parameter", line, column)
        {
            TypeExpression = typeExpression;
        }
    }
}
