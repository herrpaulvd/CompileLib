using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class SimpleStatement : Statement
    {
        public LocalVariable? LocalVariable { get; private set; }
        public Expression? Expression { get; private set; }

        public SimpleStatement(int line, int column, LocalVariable? localVariable, Expression? expression) 
            : base(line, column)
        {
            LocalVariable = localVariable;
            Expression = expression;
        }
    }
}
