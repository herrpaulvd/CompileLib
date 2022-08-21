using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Common
{
    /// <summary>
    /// Internal representation of lexems
    /// </summary>
    internal struct Token
    {
        /// <summary>
        /// Type of the lexem
        /// </summary>
        public int Type;
        /// <summary>
        /// The lexem itself
        /// </summary>
        public string Self;
        /// <summary>
        /// The line coordinate
        /// </summary>
        public int Line;
        /// <summary>
        /// The column coordinate
        /// </summary>
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
