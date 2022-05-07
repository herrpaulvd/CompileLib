using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELIntegerConst : ELExpression
    {
        private long value;
        private ELType type;

        internal ELIntegerConst(ELCompiler compiler, long value)
            :base(compiler)
        {
            this.value = value;
            type = ELType.Int64;
        }

        internal ELIntegerConst(ELCompiler compiler, ulong value)
            :base(compiler)
        {
            this.value = (long)value;
            type = ELType.UInt64;
        }

        internal ELIntegerConst(ELCompiler compiler, int value)
            :base(compiler)
        {
            this.value = value;
            type = ELType.Int32;
        }

        internal ELIntegerConst(ELCompiler compiler, uint value)
            :base(compiler)
        {
            this.value = value;
            type = ELType.UInt32;
        }

        public override ELType Type => type;
        public long SignedValue => value;
        public ulong UnsignedValue => (ulong)value;
    }
}
