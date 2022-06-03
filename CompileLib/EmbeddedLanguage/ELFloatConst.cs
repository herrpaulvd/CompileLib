using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELFloatConst : ELExpression
    {
        private double value;
        private ELType type;

        public override ELType Type => type;
        public double Value => value;

        internal ELFloatConst(ELCompiler compiler, float value)
            : base(compiler)
        {
            this.value = value;
            type = ELType.Float32;
        }

        internal ELFloatConst(ELCompiler compiler, double value)
            : base(compiler)
        {
            this.value = value;
            type = ELType.Float64;
        }
    }
}
