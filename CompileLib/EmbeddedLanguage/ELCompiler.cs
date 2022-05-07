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
        private List<ELInitializedData> data = new();

        public ELFunction? EntryPoint { get; internal set; }
        internal ELCodeContext? CurrentContext;

        public void EnterGlobal() => CurrentContext = null;

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

        public void Return(ELExpression result)
            => CurrentContext?.AddExpression(new ELReturn(result));

        public ELLabel DefineLabel() => CurrentContext?.DefineLabel() ?? new();
        public void MarkLabel(ELLabel label) => CurrentContext?.MarkLabel(label);

        public void Goto(ELLabel label) => CurrentContext?.AddExpression(new ELGoto(this, null, label));
        public void GotoIf(ELExpression condition, ELLabel label) => CurrentContext?.AddExpression(new ELGoto(this, condition, label));

        public ELExpression MakeConst(long value) => new ELIntegerConst(this, value);
        public ELExpression MakeConst(ulong value) => new ELIntegerConst(this, value);
        public ELExpression MakeConst(int value) => new ELIntegerConst(this, value);
        public ELExpression MakeConst(uint value) => new ELIntegerConst(this, value);

        private ELExpression? nullptr;
        public ELExpression NULLPTR
        {
            get
            {
                return nullptr ??= MakeConst(0).Cast(ELType.PVoid);
            }
        }

        public ELExpression AddInitializedData(ELType type, ELDataBuilder dataBuilder)
        {
            var result = new ELInitializedData(this, dataBuilder.CreateArray(), type);
            data.Add(result);
            return result;
        }

        // самое страшное: TODO: Build() method, вероятно сначала с дебаг-выводом
    }
}
