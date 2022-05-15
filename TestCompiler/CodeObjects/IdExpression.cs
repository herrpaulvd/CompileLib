using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class IdExpression : Expression
    {
        public string ID { get; private set; }

        public IdExpression(string id, int line, int column) : base(line, column)
        {
            ID = id;
        }
    }
}
