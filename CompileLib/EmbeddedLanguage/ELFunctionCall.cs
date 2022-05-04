using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELFunctionCall : ELExpression
    {
        private ELFunction function;
        private ELExpression[] args;

        public ELFunctionCall(ELFunction function, ELExpression[] args)
        {
            this.function = function;
            this.args = args;
        }

        public object Function => function;
        public int ArgumentsCount => args.Length;
        public ELExpression GetArgument(int index) => args[index];
        public override ELType Type => function.ReturnType;
    }
}
