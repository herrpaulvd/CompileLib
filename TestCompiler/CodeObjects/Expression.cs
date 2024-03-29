﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.EmbeddedLanguage;

namespace TestCompiler.CodeObjects
{
    internal abstract class Expression
    {
        public int Line { get; private set; }
        public int Column { get; private set; }

        public Expression(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public void ChangePosition(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public abstract ELExpression CompileRight(CompilationParameters compilation);
        public abstract ELMemoryCell CompileLeft(CompilationParameters compilation);
        
        public abstract TypeExpression Type { get; }
    }
}
