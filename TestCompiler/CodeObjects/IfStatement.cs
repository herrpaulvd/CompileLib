using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
