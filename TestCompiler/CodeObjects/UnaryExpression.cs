using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class UnaryExpression : Expression
    {
        public Expression Operand { get; private set; }
        public string Operation { get; private set; }

        public UnaryExpression(
            Expression operand,
            string operation,
            int line,
            int column)
            : base(line, column)
        {
            Operand = operand;
            Operation = operation;
        }
    }
}
