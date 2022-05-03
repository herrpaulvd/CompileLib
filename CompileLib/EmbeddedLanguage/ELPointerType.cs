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

        public ELPointerType(ELType baseType)
        {
            this.baseType = baseType;
        }

        public override string Name => $"Pointer<{baseType.Name}>";

        public override bool Equals(object? obj)
        {
            return obj is ELPointerType type &&
                   baseType == type.baseType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(baseType);
        }
    }
}
