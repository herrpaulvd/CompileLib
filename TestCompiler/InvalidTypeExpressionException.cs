using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler
{
    internal class InvalidTypeExpressionException : CompilationError
    {
        public InvalidTypeExpressionException(int line, int column)
            : base($"Invalid type expression at {line}:{column}") { }
    }
}
