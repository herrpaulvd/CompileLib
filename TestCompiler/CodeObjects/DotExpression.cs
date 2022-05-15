using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class DotExpression : Expression
    {
        public Expression Left { get; private set; }
        public string Right { get; private set; }
        public string Operation { get; private set; }

        public DotExpression(
            Expression left,
            string right,
            string operation,
            int line,
            int column)
            : base(line, column)
        {
            Left = left;
            Right = right;
            Operation = operation;
        }
    }
}
