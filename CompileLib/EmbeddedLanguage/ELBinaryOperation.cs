using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal enum BinaryOperationType
    {
        MOV,
        ADD,
        SUB,
        MUL,
        DIV
    }

    internal class ELBinaryOperation : ELExpression
    {
        public ELExpression Left { get; private set; }
        public ELExpression Right { get; private set; }
        public BinaryOperationType Operation { get; private set; }
        private ELType type;

        private static ELType? CheckCommonArithmetic(ELType left, ELType right)
        {
            if(left.IsAssignableTo(ELType.Int64) && right.IsAssignableTo(ELType.Int64))
                return ELType.Int64;
            if(left.IsAssignableTo(ELType.UInt64) && right.IsAssignableTo(ELType.UInt64))
                return ELType.UInt64;
            return null;
        }

        private static ELType? CheckPointerArithmetic(ELType left, ELType right)
        {
            if (left is ELPointerType && right.IsAssignableTo(ELType.Int64) || right.IsAssignableTo(ELType.UInt64))
                return left;
            if (right is ELPointerType && left.IsAssignableTo(ELType.Int64) || left.IsAssignableTo(ELType.UInt64))
                return right;
            return null;
        }

        public ELBinaryOperation(ELExpression left, ELExpression right, BinaryOperationType operation)
            : base(left.compiler)
        {
            var l = left.Type;
            var r = right.Type;

            type = operation switch
            {
                BinaryOperationType.MOV
                    => (r.IsAssignableTo(l) ? ELType.Void : throw new ArgumentException($"{r} cannot be assigned to {l}")),
                BinaryOperationType.ADD
                or BinaryOperationType.SUB
                    => CheckCommonArithmetic(l, r) ?? CheckPointerArithmetic(l, r) ?? throw new ArgumentException($"Incompatible types: {left.Type} and {right.Type}"),
                BinaryOperationType.MUL
                or BinaryOperationType.DIV
                    => CheckCommonArithmetic(l, r) ?? throw new ArgumentException($"Incompatible types: {left.Type} and {right.Type}"),
                _
                    => throw new NotImplementedException()
            };

            Left = left;
            Right = right;
            Operation = operation;
        }

        public override ELType Type => type;
    }
}
