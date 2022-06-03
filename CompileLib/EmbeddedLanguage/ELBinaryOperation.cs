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
        DIV,
        MOD,
        LESS,
        GREATER,
        LESSEQ,
        GREATEREQ,
        AND,
        OR,
        XOR,
        SL,
        SR,
        EQ,
        NEQ
    }

    internal class ELBinaryOperation : ELExpression
    {
        public ELExpression Left { get; private set; }
        public ELExpression Right { get; private set; }
        public BinaryOperationType Operation { get; private set; }
        private ELType type;

        private static ELType? CheckIntegerArithmetic(ELType left, ELType right)
        {
            if(left.IsAssignableTo(ELType.Int64) && right.IsAssignableTo(ELType.Int64))
                return ELType.Int64;
            if(left.IsAssignableTo(ELType.UInt64) && right.IsAssignableTo(ELType.UInt64))
                return ELType.UInt64;
            return null;
        }

        private static ELType? CheckFloatArithmetic(ELType left, ELType right)
        {
            if(left.IsAssignableTo(ELType.Float64) && right.IsAssignableTo(ELType.Float64))
                return ELType.Float64;
            return null;
        }

        private static ELType? CheckPointerArithmetic(ELType left, ELType right)
        {
            if (left is ELPointerType pt && pt.BaseType.Size > 0 && (right.IsAssignableTo(ELType.Int64) || right.IsAssignableTo(ELType.UInt64)))
                return left;
            return null;
        }

        private static ELType? CheckBitArithmetic(ELType left, ELType right)
        {
            if ((left.IsAssignableTo(ELType.Int64) || left.IsAssignableTo(ELType.UInt64))
                && (right.IsAssignableTo(ELType.Int64) || right.IsAssignableTo(ELType.UInt64)))
                return ELType.UInt64;
            return null;
        }

        private static ELType? CheckShiftArithmetic(ELType left, ELType right)
        {
            if (!right.IsAssignableTo(ELType.Int64) && !right.IsAssignableTo(ELType.UInt64))
                return null;
            if(left.IsAssignableTo(ELType.Int64))
                return ELType.Int64;
            if(left.IsAssignableTo(ELType.UInt64))
                return ELType.UInt64;
            return null;
        }

        private static ELType? CheckComparsionArithmetic(ELType left, ELType right, bool ptrAllowed = false)
        {
            if (left.IsAssignableTo(ELType.Int64) && right.IsAssignableTo(ELType.Int64))
                return ELType.UInt64;
            if (left.IsAssignableTo(ELType.UInt64) && right.IsAssignableTo(ELType.UInt64))
                return ELType.UInt64;
            if (left.IsAssignableTo(ELType.Float64) && right.IsAssignableTo(ELType.Float64))
                return ELType.UInt64;
            if(ptrAllowed && left is ELPointerType && right is ELPointerType)
                return ELType.UInt64;
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
                    => CheckIntegerArithmetic(l, r) 
                    ?? CheckFloatArithmetic(l, r) 
                    ?? CheckPointerArithmetic(l, r) 
                    ?? throw new ArgumentException($"Incompatible types: {left.Type} and {right.Type}"),
                BinaryOperationType.MUL
                or BinaryOperationType.DIV
                    => CheckIntegerArithmetic(l, r) ?? CheckFloatArithmetic(l, r) ?? throw new ArgumentException($"Incompatible types: {left.Type} and {right.Type}"),
                BinaryOperationType.MOD
                    => CheckIntegerArithmetic(l, r) ?? throw new ArgumentException($"Incompatible types: {left.Type} and {right.Type}"),
                BinaryOperationType.AND
                or BinaryOperationType.OR
                or BinaryOperationType.XOR
                    => CheckBitArithmetic(l, r) ?? throw new ArgumentException($"Incompatible types: {left.Type} and {right.Type}"),
                BinaryOperationType.SL
                or BinaryOperationType.SR
                    => CheckShiftArithmetic(l, r) ?? throw new ArgumentException($"Incompatible types: {left.Type} and {right.Type}"),
                BinaryOperationType.LESS
                or BinaryOperationType.GREATER
                or BinaryOperationType.LESSEQ
                or BinaryOperationType.GREATEREQ
                    => CheckComparsionArithmetic(l, r) ?? throw new ArgumentException($"Incompatible types: {left.Type} and {right.Type}"),
                BinaryOperationType.EQ
                or BinaryOperationType.NEQ
                    => CheckComparsionArithmetic(l, r, true) ?? throw new ArgumentException($"Incompatible types: {left.Type} and {right.Type}"),
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
