using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompileLib.Parsing;

namespace CompileLib.ParserTools
{
    /// <summary>
    /// Internal representation of class Parsed
    /// </summary>
    internal struct AnyParsed
    {
        internal string Tag;
        internal object? Self;
        internal int Line;
        internal int Column;

        public AnyParsed(string tag, object? self, int line, int column)
        {
            Tag = tag;
            Self = self;
            Line = line;
            Column = column;
        }
    }
}
