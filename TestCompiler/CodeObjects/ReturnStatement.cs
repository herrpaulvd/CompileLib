using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class ReturnStatement : Statement
    {
        public Expression? Expression { get; private set; }

        public ReturnStatement(int line, int column, Expression? expression)
            : base(line, column)
        {
            Expression = expression;
        }
    }
}
