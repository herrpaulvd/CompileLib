using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public class ELReference : ELExpression
    {
        private ELExpression pointer;
        internal ELExpression Pointer => pointer;

        internal ELReference(ELExpression pointer) : base(pointer.compiler)
        {
            if (pointer.Type is not ELPointerType)
                throw new ArgumentException("Cannot dereference non-pointer", nameof(pointer));

            this.pointer = pointer;
        }

        public ELExpression Address => compiler.TestContext(this, "reference") ?? compiler.AddExpression(new ELReferenceExpression(this));

        public ELExpression Value
        {
            get => compiler.TestContext(this, "reference") ?? compiler.AddExpression(new ELCopy(this));
            set
            {
                compiler.TestContext(this, "left");
                compiler.TestContext(value, "right");
                compiler.AddExpression(new ELBinaryOperation(this, value, BinaryOperationType.MOV));
            }
        }

        public override ELType Type => (pointer.Type as ELPointerType)?.BaseType ?? throw new Exception("Internal error");
    }
}
