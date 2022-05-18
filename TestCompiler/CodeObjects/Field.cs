using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;
using CompileLib.EmbeddedLanguage;

namespace TestCompiler.CodeObjects
{
    internal class Field : ClassMember
    {
        public TypeExpression TypeExpression { get; private set; }
        public int StrucFieldIndex { get; set; }
        public ELVariable? GlobalVar { get; set; }

        public Field(
            string name, 
            int line, 
            int column,
            MemberVisibility visibility,
            bool isStatic,
            TypeExpression typeExpression
            ) 
            : base(name, "field", line, column, visibility, isStatic)
        {
            TypeExpression = typeExpression;
        }
    }
}
