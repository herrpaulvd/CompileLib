using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal enum MemberVisibility
    {
        Private = 1,
        Protected = 2,
        Public = 3
    }

    internal abstract class ClassMember : CodeObject
    {
        public MemberVisibility Visibility { get; private set; }
        public bool IsStatic { get; private set; }

        public ClassMember(
            string name,
            string type,
            int line,
            int column,
            MemberVisibility visibility,
            bool isStatic
            )
            : base(name, type, line, column)
        {
            Visibility = visibility;
            IsStatic = isStatic;

            if (visibility <= MemberVisibility.Private)
            {
                AddAttribute("private");
                if(visibility <= MemberVisibility.Protected)
                {
                    AddAttribute("protected");
                    if (visibility <= MemberVisibility.Public)
                        AddAttribute("public");
                }
            }
            if (isStatic)
                AddAttribute("static");
            else
                AddAttribute("instance");
            AddAttribute("anymember");
        }
    }
}
