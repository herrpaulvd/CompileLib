using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public class ELVariable : ELExpression
    {
        private ELType type;
        private ELCompiler compiler;

        public override ELType Type => type;

        internal ELVariable(ELCompiler compiler, ELType type) : base()
        {
            this.compiler = compiler;
            this.type = type;
        }

        public ELExpression Value
        {
            get
            {
                return this;
            }
            set
            {
                compiler.CurrentContext?.AddExpression(new ELBinaryOperation(this, value, OperationType.MOV));
            }
        }
    }
}
