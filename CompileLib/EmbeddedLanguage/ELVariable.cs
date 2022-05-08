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
            if(type == ELType.Void)
                throw new ArgumentException("Cannot declare variable with the type Void", nameof(type));
            this.type = type;
        }

        public ELExpression Address => compiler.TestContext(this, "variable") ?? compiler.AddExpression(new ELReferenceExpression(this));

        public ELExpression Value
        {
            get => compiler.TestContext(this, "variable") ?? compiler.AddExpression(new ELCopy(this));
            set
            {
                compiler.TestContext(this, "left");
                compiler.TestContext(value, "right");
                compiler.AddExpression(new ELBinaryOperation(this, value, BinaryOperationType.MOV));
            }
        }

        public void SetConst(long v) => Value = compiler.MakeConst(v);
        public void SetConst(ulong v) => Value = compiler.MakeConst(v);
        public void SetConst(int v) => Value = compiler.MakeConst(v);
        public void SetConst(uint v) => Value = compiler.MakeConst(v);
    }
}
