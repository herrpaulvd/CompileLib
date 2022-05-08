using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class InvalidContextException : Exception
    {
        public InvalidContextException(string msg) : base(msg) { }
    }
}
