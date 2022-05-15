using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class Class : CodeObject
    {
        public Expression? BaseClassExpr { get; private set; }
        public Expression[] Parameters { get; private set; }
        public CodeObject[] Members { get; private set; }

        public Class(
            string name,
            int line,
            int column,
            Expression? baseClassExpr,
            string? visMod,
            Expression[] parameters,
            CodeObject[] members) 
            : base(name, "class", line, column)
        {
            BaseClassExpr = baseClassExpr;
            if(visMod is not null)
                AddAttribute(visMod);

            Parameters = parameters;
            Members = members;
            foreach (var member in members)
            {
                member.AddRelation("parent", this);
                AddRelation("child", member);
            }
        }
    }
}
