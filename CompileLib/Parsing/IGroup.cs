using CompileLib.ParserTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Interface helping in syntax analysis with Optional and Many attributes
    /// </summary>
    internal interface IGroup
    {
        IEnumerable<AnyParsed> Expand();
    }
}
