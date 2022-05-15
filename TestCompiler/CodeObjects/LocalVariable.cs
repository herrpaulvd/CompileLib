﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class LocalVariable : CodeObject
    {
        public Expression TypeExpression { get; private set; }

        public LocalVariable(string name, int line, int column, Expression typeExpression) 
            : base(name, "local-var", line, column)
        {
            TypeExpression = typeExpression;
        }
    }
}