using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELInitializedData : ELExpression
    {
        private byte[] values;
        private ELType type;

        public ELInitializedData(ELCompiler compiler, byte[] values, ELType type)
            : base(compiler)
        {
            this.values = values;
            this.type = type;
        }

        public override ELType Type => type;
        public byte[] Values => values;
    }
}
