using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class Field : CodeObject
    {
        public Expression TypeExpression { get; private set; }
        public Expression? InitExpression { get; private set; }

        public Field(
            string name, 
            int line, 
            int column,
            string? visMod,
            string? statMod,
            Expression typeExpression,
            Expression? initExpression
            ) 
            : base(name, "field", line, column)
        {
            TypeExpression = typeExpression;
            InitExpression = initExpression;
            if (visMod is not null)
                AddAttribute(visMod);
            if (statMod is not null)
                AddAttribute(statMod);
        }
    }
}
