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
        public TypeExpression TypeExpression { get; private set; }
        public Expression? InitExpression { get; private set; }
        public int StrucFieldIndex { get; set; }

        public Field(
            string name, 
            int line, 
            int column,
            MemberVisibility visibility,
            bool isStatic,
            TypeExpression typeExpression,
            Expression? initExpression
            ) 
            : base(name, "field", line, column, visibility, isStatic)
        {
            TypeExpression = typeExpression;
            InitExpression = initExpression;
        }
    }
}
