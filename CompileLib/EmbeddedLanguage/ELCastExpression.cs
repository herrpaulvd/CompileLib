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
            if (operand.Type is not ELAtomType && operand.Type is not ELPointerType)
                throw new ArgumentException("Operand to be casted must have atom or pointer type", nameof(operand));
            if (targetType is not ELAtomType && targetType is not ELPointerType)
                throw new ArgumentException("Target type must be atom or pointer type", nameof(operand));

            this.operand = operand;
            this.targetType = targetType;
        }
    }
}
