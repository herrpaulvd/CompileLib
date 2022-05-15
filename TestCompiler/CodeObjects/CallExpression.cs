using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class CallExpression : Expression
    {
        public Expression Callee { get; private set; }
        public Expression[] Args { get; private set; }
        public string BracketType { get; private set; }

        public CallExpression(
            Expression callee,
            Expression[] args,
            string bracketType,
            int line,
            int column)
            : base(line, column)
        {
            Callee = callee;
            Args = args;
            BracketType = bracketType;
        }
    }
}
