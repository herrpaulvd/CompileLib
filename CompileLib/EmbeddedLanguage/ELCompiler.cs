using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.QuasiAsm;

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

        private bool InvalidContext(int context) => context != globalContext && context != currentContext;

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
            => expression.compiler != this || InvalidContext(expr2context[expression.ID]) ? throw new ArgumentException("The operand has other context than the expression", name) : null;

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
            int funCount = functions.Count;
            int exprCount = exprs.Count;
            //int labelCount = labelAddress.Count;
            var assembler = new Assembler();
            AsmFunction[] asmf = new AsmFunction[funCount + 1];
            AsmOperand[] expr2operand = new AsmOperand[exprCount];
            //AsmOperand[] label2operand = new AsmOperand[labelCount];
            int[] expr2label = new int[exprCount];
            Array.Fill(expr2label, -1);

            asmf[0] = new(false, false, 0);
            for(int i = 0; i < funCount; i++)
                if(functions[i].Dll is null)
                {
                    var elf = functions[i];
                    var t = elf.ReturnType;
                    var f = asmf[i + 1] = new(t is ELStructType, t is ELAtomType a0 && a0.Signed, t.Size);
                    for(int j = 0; j < elf.ParametersCount; j++)
                    {
                        var p = elf.GetParameter(j);
                        t = p.Type;
                        expr2operand[p.ID] = f.AddParameter(t is ELStructType, t is ELAtomType a1 && a1.Signed, t.Size);
                    }
                }

            foreach(var e in exprs)
            {
                /*
                 * TODO: list of exprs to perform
                 * 
                 * NB!!! label
                 * 
                 * binary
                 * cast
                 * copy
                 * fieldref
                 * funcall
                 * goto + gotoif
                 * int const
                 * reference +
                 * ref expr
                 * return
                 * unary
                 * variable +
                 * 
                 * init data +
                 * 
                 */

                AsmOperand GetReference(AsmOperand op)
                {
                    if (op.IsDeref()) return op.WithUse(AsmOperandUse.Val);
                    if (op.IsVal()) return op.WithUse(AsmOperandUse.Ref);
                    throw new Exception("Internal error");
                }

                AsmOperand Dereference(AsmOperand op, ELType newType, AsmFunction f)
                {
                    if (op.IsRef()) return op.WithUse(AsmOperandUse.Val);
                    if (op.IsVal()) return op.WithUse(AsmOperandUse.Deref);
                    if(op.IsDeref())
                    {
                        // TODO: type is required
                        // TODO: по-видимому, нужно переназначать размеры
                        // т.е. для *v указывать размер не v, а именно *v
                        // пока ситуация обстоит иначе
                    }
                }

                int id = e.ID;
                if (!expr2operand[id].IsUndefined()) continue;
                int context = expr2context[id];
                var t = e.Type;

                if(context == globalContext)
                {
                    if(e is ELVariable v)
                    {
                        expr2operand[id] = assembler.AddGlobal(t is ELStructType, t is ELAtomType a0 && a0.Signed, t.Size);
                    }
                    else if(e is ELInitializedData d)
                    {
                        expr2operand[id] = assembler.AddInitData(d.Values);
                    }
                }
                else
                {
                    var f = asmf[id];
                    expr2label[id] = f.GetIP();

                    if(e is ELVariable variable)
                    {
                        expr2operand[id] = f.AddLocal(t is ELStructType, t is ELAtomType a0 && a0.Signed, t.Size);
                    }
                    else if(e is ELReference reference)
                    {
                        var res = f.AddLocal(false, false, Assembler.PtrSize);
                        f.AddOperation(
                            Assembler.MOV,
                            expr2operand[reference.Pointer.ID],
                            res);
                        expr2operand[e.ID] = res.WithUse(AsmOperandUse.Deref);
                    }
                    else if(e is ELFieldReference fieldRef)
                    {
                        var res = f.AddLocal(false, false, Assembler.PtrSize);
                        var offset = assembler.AddConst(fieldRef.FieldOffset, false, Assembler.PtrSize);
                        // TODO ADD
                    }
                }
            }
        }
    }
}
