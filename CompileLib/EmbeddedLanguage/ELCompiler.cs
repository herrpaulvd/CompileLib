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
        private List<ELExpression> exprs = new();
        private List<int> expr2context = new();
        private List<int> labelAddress = new();
        private List<ELInitializedData> data = new();

        private const int globalContext = -1;
        private const int entryPointContext = 0;
        private const int funcOffset = 1;
        private int currentContext = entryPointContext;

        public void OpenEntryPoint() => currentContext = entryPointContext;
        internal void Open(int context) => currentContext = context;

        private ELExpression AddExpression(ELExpression expression, int context)
        {
            expression.ID = exprs.Count;
            exprs.Add(expression);
            expr2context.Add(context);
            return expression;
        }

        internal ELExpression AddExpression(ELExpression expression)
            => AddExpression(expression, currentContext);

        private ELVariable AddVariable(ELType type, int context)
            => (ELVariable)AddExpression(new ELVariable(this, type));

        public ELVariable AddGlobalVariable(ELType type) => AddVariable(type, globalContext);
        public ELVariable AddLocalVariable(ELType type) => AddVariable(type, currentContext);

        public ELFunction CreateFunction(ELType returnType, params ELType[] parameterTypes)
        {
            int context = functions.Count + funcOffset;
            var parameters = parameterTypes.Select(t => (ELVariable)AddExpression(new ELVariable(this, t), context)).ToArray();
            ELFunction result = new(this, context, returnType, parameters);
            functions.Add(result);
            return result;
        }

        public ELFunction ImportFunction(string dll, string name, ELType returnType, params ELType[] parameterTypes)
        {
            int context = functions.Count + funcOffset;
            var parameters = parameterTypes.Select(t => (ELVariable)AddExpression(new ELVariable(this, t), context)).ToArray();
            ELFunction result = new(this, context, dll, name, returnType, parameters);
            functions.Add(result);
            return result;
        }

        private bool InvalidContext(int context) => context != -1 && context != currentContext;

        public ELLabel DefineLabel()
        {
            int id = labelAddress.Count;
            labelAddress.Add(-1);
            return new ELLabel(currentContext, id);
        }

        public void MarkLabel(ELLabel label)
        {
            if (InvalidContext(label.Context))
                throw new InvalidContextException("Marking label from one context at another");
            labelAddress[label.ID] = exprs.Count;
        }

        internal ELExpression? TestContext(ELExpression expression, string name)
            => expression.compiler != this && InvalidContext(expr2context[expression.ID]) ? throw new ArgumentException("The operand has other context than the expression", name) : null;

        public void Return(ELExpression result)
        {
            TestContext(result, nameof(result));
            AddExpression(new ELReturn(result));
        }

        public void Goto(ELLabel label)
        {
            if (InvalidContext(label.Context))
                throw new InvalidContextException("Jump to label from one context to another");
            AddExpression(new ELGoto(this, null, label));
        }

        public void GotoIf(ELExpression condition, ELLabel label)
        {
            if (InvalidContext(label.Context))
                throw new InvalidContextException("Jump to label from one context to another");
            TestContext(condition, nameof(condition));
            AddExpression(new ELGoto(this, condition, label));
        }

        public ELExpression MakeConst(long value) => AddExpression(new ELIntegerConst(this, value), globalContext);
        public ELExpression MakeConst(ulong value) => AddExpression(new ELIntegerConst(this, value), globalContext);
        public ELExpression MakeConst(int value) => AddExpression(new ELIntegerConst(this, value), globalContext);
        public ELExpression MakeConst(uint value) => AddExpression(new ELIntegerConst(this, value), globalContext);

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
            return AddExpression(result, globalContext);
        }

        public void BuildAndSave(string filename)
        {
            // TODO
        }
    }
}
