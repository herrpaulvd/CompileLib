using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Common
{
    internal struct Token
    {
        public int Type;
        public string Self;
        public int Line;
        public int Column;

        public Token(int type, string self, int line, int column)
        {
            Type = type;
            Self = self;
            Line = line;
            Column = column;
        }
    }
}
