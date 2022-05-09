using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public abstract class ELMemoryCell : ELExpression
    {
        public ELMemoryCell(ELCompiler compiler)
            : base(compiler) { }

        public ELExpression Address => compiler.TestContext(this, "memory") ?? compiler.AddExpression(new ELReferenceExpression(this));

        public ELExpression Value
        {
            get => compiler.TestContext(this, "memory") ?? compiler.AddExpression(new ELCopy(this));
            set
            {
                compiler.TestContext(this, "left");
                compiler.TestContext(value, "right");
                compiler.AddExpression(new ELBinaryOperation(this, value, BinaryOperationType.MOV));
            }
        }

        public ELFieldReference FieldRef(int fieldIndex)
            => (ELFieldReference?)compiler.TestContext(this, "operand") ?? (ELFieldReference)compiler.AddExpression(new ELFieldReference(fieldIndex, this));
    }
}
