using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public class ELReference : ELMemoryCell
    {
        private ELExpression pointer;
        internal ELExpression Pointer => pointer;

        internal ELReference(ELExpression pointer) : base(pointer.compiler)
        {
            if (pointer.Type is not ELPointerType)
                throw new ArgumentException("Cannot dereference non-pointer", nameof(pointer));

            this.pointer = pointer;
        }

        public override ELType Type => (pointer.Type as ELPointerType)?.BaseType ?? throw new Exception("Internal error");
    }
}
