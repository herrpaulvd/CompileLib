using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.EmbeddedLanguage;

namespace TestCompiler.CodeObjects
{
    internal class SimpleStatement : Statement
    {
        public LocalVariable? LocalVariable { get; private set; }
        public Expression? Expression { get; private set; }

        public SimpleStatement(int line, int column, LocalVariable? localVariable, Expression? expression) 
            : base(line, column)
        {
            LocalVariable = localVariable;
            Expression = expression;
        }

        public override void Compile(CompilationParameters compilation)
        {
            var name2class = compilation.Name2Class;
            var expr = Expression?.CompileRight(compilation);
            var texpr = Expression?.Type;

            if(LocalVariable is not null)
            {
                var vtype = LocalVariable.TypeExpression;
                if (vtype.IsVoid() || !name2class.ContainsKey(vtype.ClassName))
                    throw new CompilationError("Invalid variable type", Line, Column);
                ELVariable v = compilation.Compiler.AddLocalVariable(vtype.GetResolvedType(name2class));
                
                if(texpr is not null)
                {
                    if (texpr.IsAssignableTo(vtype, name2class))
                        v.Value = expr;
                    else
                        throw new CompilationError($"Cannot assign value typeof {texpr.Show(name2class)} to variable typeof {vtype.Show(name2class)}", Line, Column);
                }

                var scope = compilation.Scope;
                var prevs = compilation.SemanticNetwork.Search(compilation.Scope, "@scope-search", LocalVariable.Name);
                if (prevs.Count > 0)
                    throw new CompilationError($"Variable with the name {LocalVariable.Name} already exists", Line, Column);

                scope.AddRelation("local-var", LocalVariable);
                LocalVariable.AddRelation("parent", scope);
                LocalVariable.Variable = v;
            }
        }
    }
}
