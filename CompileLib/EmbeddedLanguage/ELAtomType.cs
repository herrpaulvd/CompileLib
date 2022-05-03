using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELAtomType : ELType
    {
        private string name;

        public override string Name => name;

        public ELAtomType(string name)
        {
            this.name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is ELAtomType type &&
                   name == type.name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(name);
        }
    }
}
