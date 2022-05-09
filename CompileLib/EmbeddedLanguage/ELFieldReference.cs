using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public class ELFieldReference : ELMemoryCell
    {
        private int fieldIndex;
        private ELMemoryCell operand;

        internal ELFieldReference(int fieldIndex, ELMemoryCell operand)
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

        public override ELType Type => (operand.Type as ELStructType)?.GetFieldType(fieldIndex) ?? throw new Exception("Internal error");
        internal int FieldIndex => fieldIndex;
        internal ELMemoryCell Operand => operand;
        internal int FieldOffset => (operand.Type as ELStructType)?.GetFieldOffset(fieldIndex) ?? throw new Exception("Internal error");
    }
}
