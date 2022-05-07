using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELGoto : ELExpression
    {
        private ELExpression? condition;
        private ELLabel target;
        public ELExpression? Condition => condition;
        public ELLabel Target => target;

        public override ELType Type => ELType.Void;

        public ELGoto(ELCompiler compiler, ELExpression? condition, ELLabel target)
            :base(compiler)
        {
            this.condition = condition;
            this.target = target;
        }
    }
}
