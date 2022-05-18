using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class ReturnStatement : Statement
    {
        public Expression? Expression { get; private set; }

        public ReturnStatement(int line, int column, Expression? expression)
            : base(line, column)
        {
            Expression = expression;
        }

        public override void Compile(CompilationParameters compilation)
        {
            var methods = compilation.SemanticNetwork.Search(compilation.Scope, "@find-parent-method");
            if (methods.Count != 1) throw new Exception("Internal error");
            var m = (Method)methods[0].Result;
            if(m.TypeExpression.IsVoid())
            {
                if (Expression is not null)
                    throw new CompilationError("Cannot return any value from void method", Line, Column);
                compilation.Compiler.Return();
            }
            else
            {
                var name2class = compilation.Name2Class;
                if (Expression is null)
                    throw new CompilationError("Value expected", Line, Column);
                var compiled = Expression.CompileRight(compilation);
                var t = Expression.Type;
                if (!t.IsAssignableTo(m.TypeExpression, name2class))
                    throw new CompilationError($"Cannot return value typeof {t.Show(name2class)} from method {m.TypeExpression.Show(name2class)}");
                compilation.Compiler.Return(compiled);
            }
        }
    }
}
