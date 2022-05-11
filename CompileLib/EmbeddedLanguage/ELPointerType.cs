using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELPointerType : ELType
    {
        private ELType baseType;
        public ELType BaseType => baseType;

        public ELPointerType(ELType baseType)
        {
            this.baseType = baseType;
        }

        public override int Size => QuasiAsm.Assembler.PtrSize;

        public override bool Equals(object? obj)
        {
            return obj is ELPointerType type &&
                   baseType == type.baseType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine("ptr", baseType);
        }

        public override bool IsAssignableTo(ELType type)
        {
            return type == this || type == PVoid;
        }

        public override string ToString()
        {
            return $"Pointer<{baseType}>";
        }
    }
}
