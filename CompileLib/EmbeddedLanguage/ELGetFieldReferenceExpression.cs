using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELGetFieldReferenceExpression : ELExpression
    {
        private int fieldIndex;
        private ELExpression operand;
        private ELType type;

        public ELGetFieldReferenceExpression(int fieldIndex, ELExpression operand)
            : base(operand.compiler)
        {
            this.fieldIndex = fieldIndex;
            this.operand = operand;
            type = ((operand.Type as ELPointerType)?.BaseType as ELStructType)?.GetFieldType(fieldIndex)?.MakePointer() ?? throw new Exception("Internal error");
        }

        public override ELType Type => type;
        public int FieldIndex => fieldIndex;
        public ELExpression Operand => operand;
    }
}
