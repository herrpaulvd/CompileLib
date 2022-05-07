using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELAtomType : ELType
    {
        private bool signed;
        private int size;
        public override int Size => size;
        public bool Signed => signed;

        public ELAtomType(int size, bool signed)
        {
            this.size = size;
            this.signed = signed;
        }

        public override bool Equals(object? obj)
        {
            return obj is ELAtomType type &&
                   signed == type.signed && size == type.size;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(signed, size);
        }

        public override bool IsAssignableTo(ELType type)
            => size > 0 && type is ELAtomType other && signed == other.signed && size <= other.size;

        public override string ToString()
        {
            if (size == 0) return "Void";
            return (signed ? "Int" : "UInt") + (size * 8).ToString();
        }
    }
}
