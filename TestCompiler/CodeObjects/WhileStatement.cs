using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class WhileStatement : Statement
    {
        public Expression Condition { get; private set; }
        public Statement Body { get; private set; }
        public bool PostCondition { get; private set; }

        public WhileStatement(
            int line,
            int column,
            Expression condition,
            Statement body,
            bool postCondition
            )
            : base(line, column)
        {
            Condition = condition;
            Body = body;
            PostCondition = postCondition;
        }

        public override void Compile(CompilationParameters compilation)
        {
            var name2class = compilation.Name2Class;
            var compiler = compilation.Compiler;

            if(PostCondition)
            {
                var startlabel = compiler.DefineLabel();
                var endlabel = compiler.DefineLabel();
                var bodylabel = compiler.DefineLabel();

                compiler.MarkLabel(bodylabel);
                CodeObject ifscope = new("", "scope", -1, -1);
                ifscope.AddRelation("parent", compilation.Scope);
                Body.Compile(compilation.WithLoop(ifscope, startlabel, endlabel));
                compiler.MarkLabel(startlabel);
                var expr = Condition.CompileRight(compilation);
                var texpr = Condition.Type;
                if (!texpr.IsIntegerType(name2class))
                    throw new CompilationError($"Invalid condition type {texpr.Show(name2class)}", Line, Column);
                compiler.GotoIf(expr, bodylabel);
                compiler.MarkLabel(endlabel);
            }
            else
            {
                var startlabel = compiler.DefineLabel();
                var endlabel = compiler.DefineLabel();

                compiler.MarkLabel(startlabel);
                var expr = Condition.CompileRight(compilation);
                var texpr = Condition.Type;
                if (!texpr.IsIntegerType(name2class))
                    throw new CompilationError($"Invalid condition type {texpr.Show(name2class)}", Line, Column);
                compiler.GotoIf(!expr, endlabel);
                CodeObject ifscope = new("", "scope", -1, -1);
                ifscope.AddRelation("parent", compilation.Scope);
                Body.Compile(compilation.WithLoop(ifscope, startlabel, endlabel));
                compiler.Goto(startlabel);
                compiler.MarkLabel(endlabel);
            }
        }
    }
}
