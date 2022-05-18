using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class ForStatement : Statement
    {
        public Statement Initialization { get; private set; }
        public Expression Condition { get; private set; }
        public Statement Step { get; private set; }
        public Statement Body { get; private set; }

        public ForStatement(
            int line,
            int column,
            Statement initialization,
            Expression condition,
            Statement step,
            Statement body
            )
            : base(line, column)
        {
            Initialization = initialization;
            Condition = condition;
            Step = step;
            Body = body;
        }

        public override void Compile(CompilationParameters compilation)
        {
            var name2class = compilation.Name2Class;
            var compiler = compilation.Compiler;

            CodeObject initscope = new("", "scope", -1, -1);
            initscope.AddRelation("parent", compilation.Scope);
            Initialization.Compile(compilation.WithScope(initscope));

            var startlabel = compiler.DefineLabel();
            var endlabel = compiler.DefineLabel();
            var contlabel = compiler.DefineLabel();

            compiler.MarkLabel(startlabel);
            var expr = Condition.CompileRight(compilation.WithScope(initscope));
            var texpr = Condition.Type;
            if (!texpr.IsIntegerType(name2class))
                throw new CompilationError($"Invalid condition type {texpr.Show(name2class)}", Line, Column);
            compiler.GotoIf(!expr, endlabel);
            Body.Compile(compilation.WithLoop(initscope, contlabel, endlabel));
            compiler.MarkLabel(contlabel);
            Step.Compile(compilation.WithScope(initscope));
            compiler.Goto(startlabel);
            compiler.MarkLabel(endlabel);
        }
    }
}
