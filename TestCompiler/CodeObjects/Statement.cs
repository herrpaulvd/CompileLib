using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal abstract class Statement
    {
        public int Line { get; private set; }
        public int Column { get; private set; }

        public Statement(int line, int column)
        {
            Line = line;
            Column = column;
        }
    }
}
