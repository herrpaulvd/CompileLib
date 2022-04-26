using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Token class
    /// </summary>
    public class Token
    {
        public string Tag { get; }
        public string Self { get; }
        public int Line { get; }
        public int Column { get; }
        public bool TypeIsUnknown => Tag == SpecialTags.TAG_UNKNOWN;

        public Token(string tag, string self, int line, int column)
        {
            Tag = tag;
            Self = self;
            Line = line;
            Column = column;
        }
    }
}
