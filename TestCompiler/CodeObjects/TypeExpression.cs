using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.EmbeddedLanguage;

namespace TestCompiler.CodeObjects
{
    internal class TypeExpression
    {
        public int Line { get; private set; }
        public int Column { get; private set; }
        public string ClassName { get; private set; }
        public int PointerDepth { get; private set; }
        private Class? resolvedClass;
        private ELType? resolvedType;
        private ELType? strucType;
        private ELType? pStrucType;

        public TypeExpression(int line, int column, string className, int pointerDepth)
        {
            Line = line;
            Column = column;
            ClassName = className;
            PointerDepth = pointerDepth;
        }

        public bool IsVoid() => ClassName == "void" && PointerDepth == 0;

        public ELType GetResolvedType(SortedDictionary<string, Class> name2class)
        {
            if(resolvedType is null || resolvedClass is null)
            {
                resolvedClass = name2class[ClassName];
                resolvedType = resolvedClass.TargetType;
                for (int i = 0; i < PointerDepth; i++)
                    resolvedType = resolvedType.MakePointer();
            }
            return resolvedType;
        }

        public ELType GetCastType(SortedDictionary<string, Class> name2class)
        {
            if (resolvedClass is null)
                GetResolvedType(name2class);
            if (strucType is null || pStrucType is null)
            {
                strucType = resolvedClass?.StrucType;
                pStrucType = strucType?.MakePointer();
            }
            return pStrucType;
        }

        public bool IsAssignableTo(TypeExpression other, SortedDictionary<string, Class> name2class)
        {
            GetResolvedType(name2class);
            other.GetResolvedType(name2class);

            if (ClassName == other.ClassName && PointerDepth == other.PointerDepth)
                return true;

            if (PointerDepth > 0)
                return other.PointerDepth == 1 && other.ClassName == "void";

            if(PointerDepth == 0
                && other.PointerDepth == 0 
                &&!resolvedClass.IsPredefined 
                && !other.resolvedClass.IsPredefined)
                return resolvedClass.Inherits(other.resolvedClass);

            if(resolvedClass.IsPredefined && other.resolvedClass.IsPredefined)
            {
                var t1 = resolvedClass.TargetType;
                var t2 = other.resolvedClass.TargetType;
                return resolvedClass.IsSigned == other.resolvedClass.IsSigned 
                    && resolvedClass.IsFloat == other.resolvedClass.IsFloat
                    && t1.Size <= t2.Size;
            }
            return false;
        }

        public string Show(SortedDictionary<string, Class> name2class)
        {
            GetResolvedType(name2class);
            StringBuilder result = new(resolvedClass.Name);
            for (int i = 0; i < PointerDepth; i++)
                result.Append("[]");
            return result.ToString();
        }

        public bool IsIntegerType(SortedDictionary<string, Class> name2class)
        {
            GetResolvedType(name2class);
            return !IsVoid() && resolvedClass.IsPredefined && !resolvedClass.IsFloat;
        }

        public bool IsFloatType(SortedDictionary<string, Class> name2class)
        {
            GetResolvedType(name2class);
            return !IsVoid() && resolvedClass.IsPredefined && resolvedClass.IsFloat;
        }
    }
}
