using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public class ELVariable : ELMemoryCell
    {
        private ELType type;

        public override ELType Type => type;

        internal ELVariable(ELCompiler compiler, ELType type) : base(compiler)
        {
            if(type == ELType.Void)
                throw new ArgumentException("Cannot declare variable with the type Void", nameof(type));
            this.type = type;
        }
    }
}
