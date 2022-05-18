using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal interface IObjectSearcher
    {
        object FindObject(CompilationParameters compilation);
    }
}
