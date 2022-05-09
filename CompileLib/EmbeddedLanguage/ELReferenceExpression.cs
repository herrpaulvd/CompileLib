using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELReferenceExpression : ELExpression
    {
        private ELType type;
        private ELMemoryCell expression;
        public ELMemoryCell Expression => expression;

        public override ELType Type => type;

        public ELReferenceExpression(ELMemoryCell expression)
            : base(expression.compiler)
        {
            this.expression = expression;
            type = expression.Type.MakePointer();
        }
    }
}
