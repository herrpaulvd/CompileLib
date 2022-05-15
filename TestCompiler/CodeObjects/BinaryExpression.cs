using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class BinaryExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }
        public string Operation { get; private set; }

        public BinaryExpression(
            Expression left, 
            Expression right, 
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
