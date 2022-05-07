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

        public override ELType Type => type;

        internal ELVariable(ELCompiler compiler, ELType type) : base(compiler)
        {
            this.type = type;
        }

        public ELExpression Value
        {
            set
            {
                compiler.CurrentContext?.AddExpression(new ELBinaryOperation(this, value, BinaryOperationType.MOV));
            }
        }

        public void SetConst(long v) => Value = compiler.MakeConst(v);
        public void SetConst(ulong v) => Value = compiler.MakeConst(v);
        public void SetConst(int v) => Value = compiler.MakeConst(v);
        public void SetConst(uint v) => Value = compiler.MakeConst(v);
    }
}
