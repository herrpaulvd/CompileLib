using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Exception throwed by ParsingEngineBuilder
    /// </summary>
    public class ParsingEngineBuildingException : Exception
    {
        public ParsingEngineBuildingException(string msg) : base(msg)
        { }
    }
}
