using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;
using CompileLib.EmbeddedLanguage;

namespace TestCompiler.CodeObjects
{
    internal class LocalVariable : CodeObject
    {
        public TypeExpression TypeExpression { get; private set; }
        public ELVariable Variable { get; set; }

        public LocalVariable(string name, int line, int column, TypeExpression typeExpression) 
            : base(name, "local-var", line, column)
        {
            TypeExpression = typeExpression;
        }
    }
}
