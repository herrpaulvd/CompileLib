﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public abstract class ELType
    {
        public static readonly ELType Int8 = new ELAtomType(1, true, false);
        public static readonly ELType Int16 = new ELAtomType(2, true, false);
        public static readonly ELType Int32 = new ELAtomType(4, true, false);
        public static readonly ELType Int64 = new ELAtomType(8, true, false);

        public static readonly ELType UInt8 = new ELAtomType(1, false, false);
        public static readonly ELType UInt16 = new ELAtomType(2, false, false);
        public static readonly ELType UInt32 = new ELAtomType(4, false, false);
        public static readonly ELType UInt64 = new ELAtomType(8, false, false);

        public static readonly ELType Void = new ELAtomType(0, false, false);
        public static readonly ELType PVoid = Void.MakePointer();

        public static readonly ELType Float32 = new ELAtomType(4, true, true);
        public static readonly ELType Float64 = new ELAtomType(8, true, true);

        public abstract int Size { get; }
        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();
        public abstract bool IsAssignableTo(ELType type);

        public abstract override string ToString();

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
