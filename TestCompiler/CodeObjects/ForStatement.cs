using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class ForStatement : Statement
    {
        public Expression Initialization { get; private set; }
        public Expression Condition { get; private set; }
        public Expression Step { get; private set; }
        public Statement Body { get; private set; }

        public ForStatement(
            int line,
            int column,
            Expression initialization,
            Expression condition,
            Expression step,
            Statement body
            )
            : base(line, column)
        {
            Initialization = initialization;
            Condition = condition;
            Step = step;
            Body = body;
        }
    }
}
