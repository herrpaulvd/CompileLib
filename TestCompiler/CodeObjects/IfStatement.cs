using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class IfStatement : Statement
    {
        public Expression Condition { get; private set; }
        public Statement IfBranch { get; private set; }
        public Statement? ElseBranch { get; private set; }

        public IfStatement(
            int line, 
            int column,
            Expression condition,
            Statement ifBranch,
            Statement? elseBranch
            ) 
            : base(line, column)
        {
            Condition = condition;
            IfBranch = ifBranch;
            ElseBranch = elseBranch;
        }

        public override void Compile(CompilationParameters compilation)
        {
            var name2class = compilation.Name2Class;
            var compiler = compilation.Compiler;
            var elselabel = compiler.DefineLabel();
            var endlabel = compiler.DefineLabel();

            var expr = Condition.CompileRight(compilation);
            var texpr = Condition.Type;
            if (!texpr.IsIntegerType(name2class))
                throw new CompilationError($"Invalid condition type {texpr.Show(name2class)}", Line, Column);
            compiler.GotoIf(!expr, elselabel);
            CodeObject ifscope = new("", "scope", -1, -1);
            ifscope.AddRelation("parent", compilation.Scope);
            IfBranch.Compile(compilation.WithScope(ifscope));
            compiler.Goto(endlabel);
            compiler.MarkLabel(elselabel);
            if(ElseBranch is not null)
            {
                CodeObject elsescope = new("", "scope", -1, -1);
                elsescope.AddRelation("parent", compilation.Scope);
                ElseBranch.Compile(compilation.WithScope(elsescope));
            }
            compiler.MarkLabel(endlabel);
        }
    }
}
