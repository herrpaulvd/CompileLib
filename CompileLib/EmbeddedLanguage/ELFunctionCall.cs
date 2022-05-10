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
            : base(function.Compiler)
        {
            this.function = function;
            this.args = args;
        }

        public ELFunction Function => function;
        public int ArgumentsCount => args.Length;
        public ELExpression GetArgument(int index) => args[index];
        public IEnumerable<ELExpression> AllArgs() => args;
        public override ELType Type => function.ReturnType;
    }
}
