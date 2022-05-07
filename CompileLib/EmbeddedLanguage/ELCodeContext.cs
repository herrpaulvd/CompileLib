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
        private List<int> labelAddress = new() { 0 };

        public void AddLocal(ELVariable variable)
        {
            locals.Add(variable);
        }

        public void AddExpression(ELExpression e)
        {
            evaluationSequence.Add(e);
        }

        public ELLabel DefineLabel()
        {
            int id = labelAddress.Count;
            labelAddress.Add(-1);
            return new ELLabel { ID = id };
        }

        public void MarkLabel(ELLabel label)
        {
            labelAddress[label.ID] = evaluationSequence.Count;
        }
    }
}
