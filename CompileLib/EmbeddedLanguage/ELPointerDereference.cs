using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELPointerDereference : ELExpression
    {
        private ELExpression pointer;
        public ELExpression Pointer => pointer;

        public override ELType Type => (pointer.Type as ELPointerType).BaseType;

        public ELPointerDereference(ELExpression pointer)
            : base(pointer.compiler)
        {
            if(pointer.Type is ELPointerType pointerType)
            {
                this.pointer = pointer;
            }
            else
            {
                throw new ArgumentException("Only pointer type value can be dereferenced", nameof(pointer));
            }
        }
    }
}
