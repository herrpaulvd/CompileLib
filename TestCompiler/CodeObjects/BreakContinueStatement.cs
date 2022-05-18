using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class BreakContinueStatement : Statement
    {
        public bool IsBreak { get; private set; }
        public bool IsContinue => !IsBreak;

        public BreakContinueStatement(int line, int column, bool isBreak) : base(line, column)
        {
            IsBreak = isBreak;
        }

        public override void Compile(CompilationParameters compilation)
        {
            if (!compilation.HasLabels)
                throw new CompilationError("No loop for the statement", Line, Column);
            compilation.Compiler.Goto(IsBreak ? compilation.BreakLabel : compilation.ContinueLabel);
        }
    }
}
