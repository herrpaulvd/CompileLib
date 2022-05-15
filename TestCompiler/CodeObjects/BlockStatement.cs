using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class BlockStatement : Statement
    {
        public Statement[] Statements { get; private set; }

        public BlockStatement(int line, int column, Statement[] statements) 
            : base(line, column)
        {
            Statements = statements;
        }
    }
}
