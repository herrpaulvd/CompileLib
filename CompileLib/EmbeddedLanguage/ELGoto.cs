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
            if (condition is not null && condition.Type is not ELAtomType && condition.Type is not ELPointerType)
                throw new ArgumentException("Goto condition must have either atom or pointer type", nameof(condition));
            this.condition = condition;
            this.target = target;
        }
    }
}
