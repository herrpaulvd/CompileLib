﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;
using CompileLib.EmbeddedLanguage;

namespace TestCompiler.CodeObjects
{
    internal class Parameter : CodeObject
    {
        public TypeExpression TypeExpression { get; private set; }
        public ELVariable Variable { get; set; }

        public Parameter(string name, int line, int column, TypeExpression typeExpression) 
            : base(name, "parameter", line, column)
        {
            TypeExpression = typeExpression;
        }
    }
}
