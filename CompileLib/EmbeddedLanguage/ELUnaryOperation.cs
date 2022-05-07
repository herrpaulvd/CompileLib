using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal enum UnaryOperationType
    {
        BOOLEAN_NOT,
        NEG,
        BITWISE_NOT
    }

    internal class ELUnaryOperation : ELExpression
    {
        public ELExpression Operand { get; private set; }
        public UnaryOperationType Operation { get; private set; }
        private ELType type;

        public override ELType Type => type;

        private static ELType? CheckCommonArithmetic(ELType operandType)
        {
            if (operandType.IsAssignableTo(ELType.Int64))
                return ELType.Int64;
            if (operandType.IsAssignableTo(ELType.UInt64))
                return ELType.UInt64;
            return null;
        }

        public ELUnaryOperation(ELExpression operand, UnaryOperationType operation)
            : base(operand.compiler)
        {
            var ot = operand.Type;
            type = operation switch
            {
                UnaryOperationType.BOOLEAN_NOT 
                or UnaryOperationType.NEG
                or UnaryOperationType.BITWISE_NOT
                    => CheckCommonArithmetic(ot) ?? throw new ArgumentException($"Invalid NOT operand type {ot}", nameof(operand)),
                _ => throw new NotImplementedException()
            };
            Operand = operand;
            Operation = operation;
        }
    }
}
