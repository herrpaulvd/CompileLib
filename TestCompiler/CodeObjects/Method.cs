using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class Method : ClassMember
    {
        public TypeExpression TypeExpression { get; private set; }
        public Statement MainStatement { get; private set; }
        public Parameter[] Parameters { get; private set; }
        public bool IsConstructor => Name.Length == 0;

        public Method(
            string name, 
            int line, 
            int column,
            MemberVisibility visibility,
            bool isStatic,
            TypeExpression typeExpression,
            Statement mainStatement,
            Parameter[] parameters)
            : base(name, name.Length == 0 ? "constructor" : "method", line, column, visibility, isStatic)
        {
            TypeExpression = typeExpression;
            MainStatement = mainStatement;

            Parameters = parameters;
            foreach(var p in Parameters)
            {
                AddRelation("parameter", p);
            }
        }
    }
}
