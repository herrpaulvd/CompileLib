using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    internal class ELCodeContext
    {
        private List<ELVariable> locals = new();
        private List<ELExpression> evaluationSequence = new();

        public void AddLocal(ELVariable variable)
        {
            locals.Add(variable);
        }

        public void AddExpression(ELExpression e)
        {
            evaluationSequence.Add(e);
        }
    }
}
