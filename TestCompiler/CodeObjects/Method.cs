﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class Method : CodeObject
    {
        public Expression TypeExpression { get; private set; }
        public Statement MainStatement { get; private set; }
        public Parameter[] Parameters { get; private set; }
        public bool IsConstructor => Name.Length == 0;

        public Method(
            string name, 
            int line, 
            int column,
            string? visMod,
            string? statMod,
            Expression typeExpression,
            Statement mainStatement,
            Parameter[] parameters)
            : base(name, name.Length == 0 ? "constructor" : "method", line, column)
        {
            TypeExpression = typeExpression;
            MainStatement = mainStatement;
            if (visMod is not null)
                AddAttribute(visMod);
            if (statMod is not null)
                AddAttribute(statMod);

            Parameters = parameters;
            foreach(var p in Parameters)
            {
                AddRelation("parameter", p);
            }
        }
    }
}
