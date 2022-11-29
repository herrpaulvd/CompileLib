using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.ParserTools
{
    /// <summary>
    /// internal production handler interface
    /// </summary>
    internal interface IProductionHandler
    {
        object? Handle(AnyParsed[] children, ref string tag);
    }
}
