using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class BlockStatement : CodeObject
    {
        public CodeObject[] Statements { get; private set; }

        public BlockStatement(string name, string type, int line, int column, CodeObject[] statements) 
            : base(name, type, line, column)
        {
            Statements = statements;
        }
    }
}
