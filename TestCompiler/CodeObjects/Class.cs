using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;
using CompileLib.EmbeddedLanguage;

namespace TestCompiler.CodeObjects
{
    internal class Class : CodeObject
    {
        public string? BaseClassName { get; private set; }
        public ClassMember[] Members { get; private set; }
        public bool IsPredefined { get; private set; }
        public ELType TargetType { get; set; }
        public ELStructType? StrucType { get; set; }
        public bool IsSigned { get; private set; }
        public bool IsFloat { get; private set; }

        public Class(
            string name,
            int line,
            int column,
            string? baseClassName,
            ClassMember[] members) 
            : base(name, "class", line, column)
        {
            IsSigned = false;
            TargetType = ELType.PVoid;
            IsPredefined = false;
            BaseClassName = baseClassName;
            Members = members;
            foreach (var member in members)
            {
                member.AddRelation("parent", this);
                AddRelation("child", member);
            }
        }

        public Class(
            string name,
            ClassMember[] members,
            ELType targetType,
            bool isSigned,
            bool isFloat
            )
            : base(name, "class", -1, -1)
        {
            TargetType = targetType;
            IsPredefined = true;
            Members = members;
            IsSigned = isSigned;
            IsFloat = isFloat;
            foreach (var member in members)
            {
                member.AddRelation("parent", this);
                AddRelation("child", member);
            }
        }

        public bool Inherits(Class c)
        {
            var baseclass = GetOneRelated("base-class");
            if (baseclass == c)
                return true;
                return baseclass is Class other && other.Inherits(c);
        }
    }
}
