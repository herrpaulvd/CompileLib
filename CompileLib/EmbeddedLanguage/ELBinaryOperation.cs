using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal enum OperationType
    {
        MOV
    }

    internal class ELBinaryOperation : ELExpression
    {
        public ELExpression Left { get; private set; }
        public ELExpression Right { get; private set; }
        public OperationType Operation { get; private set; }
        private ELType type;

        public ELBinaryOperation(ELExpression left, ELExpression right, OperationType operation)
        {
            type = operation switch
            {
                OperationType.MOV => (left.Type == right.Type ? left.Type : throw new ArgumentException($"Incompatible types: {left.Type} and {right.Type}")),
                _ => throw new NotImplementedException()
            };

            Left = left;
            Right = right;
            Operation = operation;
        }

        // TEMP
        public override ELType Type => type;
    }
}
