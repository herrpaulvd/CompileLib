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
        private ELExpression expression;
        public ELExpression Expression => expression;

        public override ELType Type => type;

        public ELReferenceExpression(ELExpression expression)
            : base(expression.compiler)
        {
            this.expression = expression;
            type = expression.Type.MakePointer();
        }
    }
}
