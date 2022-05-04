using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public class ELCompiler
    {
        private List<ELFunction> functions = new();
        private List<ELVariable> globalVariables = new();
        //

        public ELFunction? EntryPoint { get; internal set; }
        internal ELCodeContext? CurrentContext;

        public ELFunction CreateFunction(ELType returnType, params ELType[] parameterTypes)
        {
            ELFunction result = new(this, returnType, parameterTypes);
            functions.Add(result);
            return result;
        }

        public ELFunction ImportFunction(string dll, string name, ELType returnType, params ELType[] parameterTypes)
        {
            ELFunction result = new(this, dll, name, returnType, parameterTypes);
            functions.Add(result);
            return result;
        }

        public ELVariable AddVariable(ELType type)
        {
            ELVariable result = new(this, type);
            if (CurrentContext is null)
                globalVariables.Add(result);
            else
                CurrentContext.AddLocal(result);
            return result;
        }
    }
}
