using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class Field : ClassMember
    {
        public Expression TypeExpression { get; private set; }
        public Expression? InitExpression { get; private set; }

        public Field(
            string name, 
            int line, 
            int column,
            MemberVisibility visibility,
            bool isStatic,
            Expression typeExpression,
            Expression? initExpression
            ) 
            : base(name, "field", line, column, visibility, isStatic)
        {
            TypeExpression = typeExpression;
            InitExpression = initExpression;
        }
    }
}
