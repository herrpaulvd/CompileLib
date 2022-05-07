using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELCastExpression : ELExpression
    {
        private ELExpression operand;
        private ELType targetType;

        public ELExpression Operand => operand;
        public ELType TargetType => targetType;
        public override ELType Type => targetType;

        public ELCastExpression(ELExpression operand, ELType targetType)
            : base(operand.compiler)
        {
            this.operand = operand;
            this.targetType = targetType;
        }
    }
}
