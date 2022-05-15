using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class ConstExpression : Expression
    {
        public string Self { get; private set; }
        public string Type { get; private set; }

        public ConstExpression(string self, string type, int line, int column) : base(line, column)
        {
            Self = self;
            Type = type;
        }
    }
}
