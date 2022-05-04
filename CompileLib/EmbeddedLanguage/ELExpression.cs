using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public abstract class ELExpression
    {
        public abstract ELType Type { get; }

        public ELExpression Cast(ELType targetType)
            => new ELCastExpression(this, targetType);

        public static implicit operator ELExpression(long value)
            => new ELIntegerConst(value, ELType.Int64);

        public static implicit operator ELExpression(ulong value)
            => new ELIntegerConst((long)value, ELType.UInt64);
    }
}
