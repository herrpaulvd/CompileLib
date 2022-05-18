using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class BlockStatement : Statement
    {
        public Statement[] Statements { get; private set; }

        public BlockStatement(int line, int column, Statement[] statements) 
            : base(line, column)
        {
            Statements = statements;
        }

        public override void Compile(CompilationParameters compilation)
        {
            CodeObject scope = new("", "scope", Line, Column);
            scope.AddRelation("parent", compilation.Scope);
            compilation = compilation.WithScope(scope);
            foreach(var s in Statements)
                s.Compile(compilation);
        }
    }
}
