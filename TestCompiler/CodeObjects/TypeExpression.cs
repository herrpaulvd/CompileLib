using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class TypeExpression
    {
        public int Line { get; private set; }
        public int Column { get; private set; }
        public string ClassName { get; private set; }
        public int PointerDepth { get; private set; }

        public TypeExpression(int line, int column, string className, int pointerDepth)
        {
            Line = line;
            Column = column;
            ClassName = className;
            PointerDepth = pointerDepth;
        }

        public bool IsVoid() => ClassName == "void" && PointerDepth == 0;
    }
}
