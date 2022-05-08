using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public class ELFieldReference : ELExpression
    {
        private int fieldIndex;
        private ELExpression operand;

        internal ELFieldReference(int fieldIndex, ELExpression operand)
            : base(operand.compiler)
        {
            if(operand.Type is ELStructType st)
            {
                if (fieldIndex < 0 || fieldIndex >= st.FieldCount)
                    throw new ArgumentException("Invalid field index", nameof(operand));
            }

            this.fieldIndex = fieldIndex;
            this.operand = operand;
        }

        public ELExpression Address => compiler.TestContext(this, "fieldref") ?? compiler.AddExpression(new ELReferenceExpression(this));

        public ELExpression Value
        {
            get => compiler.TestContext(this, "fieldref") ?? compiler.AddExpression(new ELCopy(this));
            set
            {
                compiler.TestContext(this, "left");
                compiler.TestContext(value, "right");
                compiler.AddExpression(new ELBinaryOperation(this, value, BinaryOperationType.MOV));
            }
        }

        public override ELType Type => (operand.Type as ELStructType)?.GetFieldType(fieldIndex) ?? throw new Exception("Internal error");
        internal int FieldIndex => fieldIndex;
        internal ELExpression Operand => operand;
    }
}
