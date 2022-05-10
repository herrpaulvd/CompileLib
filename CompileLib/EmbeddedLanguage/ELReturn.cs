using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELReturn : ELExpression
    {
        private ELExpression? result;
        public ELExpression? Result => result;

        public ELReturn(ELExpression result)
            : base(result.compiler)
        {
            this.result = result;
        }

        public ELReturn(ELCompiler compiler) : base(compiler)
        {
            result = null;
        }

        public override ELType Type => result?.Type ?? ELType.Void;
    }
}
