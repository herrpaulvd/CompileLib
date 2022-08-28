using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Common
{
    internal struct CodeChar
    {
        public char c;
        public int line;
        public int column;

        public CodeChar(char c, int line, int column)
        {
            this.c = c;
            this.line = line;
            this.column = column;
        }
    }
}
