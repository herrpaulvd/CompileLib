using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
