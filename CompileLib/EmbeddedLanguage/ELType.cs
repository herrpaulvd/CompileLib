using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public abstract class ELType
    {
        public static readonly ELType Int8 = new ELAtomType("Int8");
        public static readonly ELType Int16 = new ELAtomType("Int16");
        public static readonly ELType Int32 = new ELAtomType("Int32");
        public static readonly ELType Int64 = new ELAtomType("Int64");

        public static readonly ELType UInt8 = new ELAtomType("UInt8");
        public static readonly ELType UInt16 = new ELAtomType("UInt16");
        public static readonly ELType UInt32 = new ELAtomType("UInt32");
        public static readonly ELType UInt64 = new ELAtomType("UInt64");

        public abstract string Name { get; }

        public static bool operator==(ELType a, ELType b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(ELType a, ELType b)
        {
            return !a.Equals(b);
        }

        public ELType MakePointer() => new ELPointerType(this);
    }
}
