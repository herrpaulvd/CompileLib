using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    // maybe temp
    public class ELIntegerConst : ELExpression
    {
        private long value;
        private ELType type;

        internal ELIntegerConst(long value, ELType type)
        {
            this.value = value;
            this.type = type;
        }

        public override ELType Type => type;
    }
}
