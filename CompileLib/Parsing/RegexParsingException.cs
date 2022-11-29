using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// An exception throwed while parsing regular expressions
    /// </summary>
    public class RegexParsingException : Exception
    {
        public int Line { get; internal set; }
        public int Column { get; internal set; }

        public RegexParsingException(string msg, int line, int column) : base($"{msg} at {line}:{column}")
        {
            Line = line;
            Column = column;
        }
    }
}
