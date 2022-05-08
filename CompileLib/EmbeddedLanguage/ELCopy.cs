using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELCopy : ELExpression
    {
        private ELExpression operand;
        public ELExpression Operand => operand;

        public ELCopy(ELExpression operand) : base(operand.compiler)
        {
            this.operand = operand;
        }

        public override ELType Type => operand.Type;
    }
}
