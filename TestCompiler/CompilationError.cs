using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler
{
    internal class CompilationError : Exception
    {
        public CompilationError(string message) : base(message) { }
        public CompilationError(string message, int line, int column) : base($"{message} at {line}:{column}") { }
    }
}
