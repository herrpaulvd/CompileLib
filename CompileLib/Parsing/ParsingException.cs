using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Exception throwed while parsing
    /// </summary>
    public class ParsingException : Exception
    {
        public ParsingException(string msg) : base(msg)
        { }
    }
}
