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

        public void Return()
        {
            if (currentContext == entryPointContext || functions[currentContext - funcOffset].ReturnType == ELType.Void)
            {
                AddExpression(new ELReturn(this));
            }
            else
                throw new ArgumentException("The opened function must return Void", "context");
        }

        public void Return(ELExpression result)
        {
            if (currentContext != entryPointContext && result.Type.IsAssignableTo(functions[currentContext - funcOffset].ReturnType))
            {
                TestContext(result, nameof(result));
                AddExpression(new ELReturn(result));
            }
            else
                throw new ArgumentException("The function cannot return " + result.Type, "context");
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
            if (type is not ELPointerType) throw new ArgumentException("Initialized data type must be always a pointer", nameof(type));
            var result = new ELInitializedData(this, dataBuilder.CreateArray(), type);
            data.Add(result);
            return AddExpression(result, globalContext);
        }

        public void BuildAndSave(string filename)
        {
            const int PtrSize = Assembler.PtrSize;

            int funCount = functions.Count;
            int exprCount = exprs.Count;
            int labelCount = labelAddress.Count;

            var assembler = new Assembler();
            AsmFunction[] asmf = new AsmFunction[funCount + 1];
            AsmOperand[] expr2operand = new AsmOperand[exprCount];
            AsmOperand[] label2operand = new AsmOperand[labelCount];
            int[] expr2ip = new int[exprCount];
            Array.Fill(expr2ip, -1);

            List<(int, int, int)> usedLabels = new();

            asmf[0] = new(false, false, 0, ELType.Void);
            for(int i = 0; i < funCount; i++)
                if(functions[i].Dll is null)
                {
                    var elf = functions[i];
                    var t = elf.ReturnType;
                    var f = asmf[i + 1] = new(t is ELStructType, t is ELAtomType a0 && a0.Signed, t.Size, t);
                    for(int j = 0; j < elf.ParametersCount; j++)
                    {
                        var p = elf.GetParameter(j);
                        t = p.Type;
                        expr2operand[p.ID] = f.AddParameter(t is ELStructType, t is ELAtomType a1 && a1.Signed, t.Size, t);
                    }
                }

            foreach(var e in exprs)
            {
                /*
                 * TODO: list of exprs to perform
                 * 
                 * NB!!! label
                 * 
                 * binary +
                 * cast +
                 * copy +
                 * fieldref +
                 * funcall +
                 * goto + gotoif +
                 * int const +
                 * reference +
                 * ref expr +
                 * return +
                 * unary +
                 * variable +
                 * 
                 * init data +
                 * 
                 */

                AsmOperand MakeReference(AsmOperand op)
                {
                    if (op.IsDeref())
                    {
                        var tup = (op.Tag as ELType).MakePointer();
                        return op.ChangeUse(AsmOperandUse.Val, false, false, PtrSize, tup);
                    }
                    if (op.IsVal())
                    {
                        var tup = (op.Tag as ELType).MakePointer();
                        return op.ChangeUse(AsmOperandUse.Ref, false, false, PtrSize, tup);
                    }
                    throw new NotImplementedException();
                }

                AsmOperand Dereference(AsmOperand op, AsmFunction f)
                {
                    if (op.IsRef())
                    {
                        var tdown = (op.Tag as ELPointerType).BaseType;
                        return op.ChangeUse(AsmOperandUse.Val, tdown is ELStructType, tdown is ELAtomType a1 && a1.Signed, tdown.Size, tdown);
                    }
                    if (op.IsVal())
                    {
                        var tdown = (op.Tag as ELPointerType).BaseType;
                        return op.ChangeUse(AsmOperandUse.Deref, tdown is ELStructType, tdown is ELAtomType a1 && a1.Signed, tdown.Size, tdown);
                    }
                    if(op.IsDeref())
                    {
                        var tdown = (op.Tag as ELPointerType).BaseType;
                        var tempVar = f.AddLocal(op.IsStruc(), op.IsSigned(), op.Size, op.Tag);
                        f.AddOperation(Assembler.MOV, tempVar, op);
                        return tempVar.ChangeUse(AsmOperandUse.Deref, tdown is ELStructType, tdown is ELAtomType a1 && a1.Signed, tdown.Size, tdown);
                    }
                    throw new NotImplementedException();
                }

                int id = e.ID;
                if (!expr2operand[id].IsUndefined()) continue;
                int context = expr2context[id];
                var t = e.Type;

                if(context == globalContext)
                {
                    if(e is ELVariable v)
                    {
                        expr2operand[id] = assembler.AddGlobal(t is ELStructType, t is ELAtomType a0 && a0.Signed, t.Size, v.Type);
                    }
                    else if(e is ELInitializedData d)
                    {
                        expr2operand[id] = assembler.AddInitData(d.Values, d.Type);
                    }
                }
                else
                {
                    var f = asmf[id];
                    expr2ip[id] = f.GetIP();

                    if (e is ELVariable variable)
                    {
                        expr2operand[id] = f.AddLocal(t is ELStructType, t is ELAtomType a0 && a0.Signed, t.Size, t);
                    }
                    else if (e is ELReference reference)
                    {
                        var res = f.AddLocal(false, false, PtrSize, e.Type.MakePointer());
                        f.AddOperation(
                            Assembler.MOV,
                            res,
                            expr2operand[reference.Pointer.ID]);
                        expr2operand[e.ID] = Dereference(res, f);
                    }
                    else if (e is ELFieldReference fieldRef)
                    {
                        var res = f.AddLocal(false, false, PtrSize, e.Type.MakePointer());
                        var offset = assembler.AddConst(fieldRef.FieldOffset, false, PtrSize, ELType.UInt64);
                        f.AddOperation(
                            Assembler.ADD,
                            res,
                            expr2operand[fieldRef.Operand.ID],
                            offset);
                        expr2operand[fieldRef.Operand.ID] = Dereference(res, f);
                    }
                    else if (e is ELIntegerConst intconst)
                    {
                        expr2operand[e.ID] = assembler.AddConst(intconst.SignedValue, t is ELAtomType a0 && a0.Signed, t.Size, t);
                    }
                    else if (e is ELReturn ret)
                    {
                        if (ret.Result is null)
                            f.AddOperation(Assembler.RET, AsmOperand.Undefined);
                        else
                            f.AddOperation(Assembler.RETVAL, AsmOperand.Undefined, expr2operand[ret.Result.ID]);
                    }
                    else if (e is ELGoto jump)
                    {
                        int labelID = jump.Target.ID;
                        if (label2operand[labelID].IsUndefined())
                        {
                            if (labelAddress[labelID] < 0)
                                throw new Exception("A label is unmarked");
                            usedLabels.Add((labelAddress[labelID], labelID, jump.Target.Context));
                            label2operand[labelID] = assembler.AddConst(0, false, PtrSize, ELType.PVoid);
                        }
                        if (jump.Condition is null)
                            f.AddOperation(Assembler.GOTO, AsmOperand.Undefined, label2operand[labelID]);
                        else
                            f.AddOperation(Assembler.GOTOIF, AsmOperand.Undefined, expr2operand[jump.Condition.ID], label2operand[labelID]);
                    }
                    else if (e is ELCastExpression cast)
                    {
                        expr2operand[e.ID] = f.AddLocal(t is ELStructType, t is ELAtomType a0 && a0.Signed, t.Size, t);
                        f.AddOperation(
                            Assembler.MOV,
                            expr2operand[e.ID],
                            expr2operand[cast.Operand.ID]);
                    }
                    else if (e is ELCopy copy)
                    {
                        expr2operand[e.ID] = f.AddLocal(t is ELStructType, t is ELAtomType a0 && a0.Signed, t.Size, t);
                        f.AddOperation(
                            Assembler.MOV,
                            expr2operand[e.ID],
                            expr2operand[copy.Operand.ID]);
                    }
                    else if (e is ELReferenceExpression refexpr)
                    {
                        expr2operand[e.ID] = MakeReference(expr2operand[refexpr.Expression.ID]);
                    }
                    else if (e is ELFunctionCall funcall)
                    {
                        if (funcall.Function.ReturnType == ELType.Void)
                            expr2operand[e.ID] = AsmOperand.Undefined;
                        else
                            expr2operand[e.ID] = f.AddLocal(t is ELStructType, t is ELAtomType a0 && a0.Signed, t.Size, t);
                        var elfun = funcall.Function;
                        if (elfun.Dll is null || elfun.Name is null)
                        {
                            f.AddOperation(
                                asmf[elfun.Context],
                                expr2operand[e.ID],
                                funcall.AllArgs().Select(a => expr2operand[a.ID]).ToArray());
                        }
                        else
                        {
                            f.AddOperation(
                                new AsmImportCall(elfun.Dll, elfun.Name),
                                expr2operand[e.ID],
                                funcall.AllArgs().Select(a => expr2operand[a.ID]).ToArray());
                        }
                    }
                    else if (e is ELUnaryOperation unary)
                    {
                        expr2operand[e.ID] = f.AddLocal(t is ELStructType, t is ELAtomType a0 && a0.Signed, t.Size, t);
                        f.AddOperation(
                            unary.Operation switch
                            {
                                UnaryOperationType.NEG => Assembler.NEG,
                                UnaryOperationType.BITWISE_NOT => Assembler.BITWISE_NOT,
                                UnaryOperationType.BOOLEAN_NOT => Assembler.BOOLEAN_NOT,
                                _ => throw new NotImplementedException()
                            },
                            expr2operand[e.ID],
                            expr2operand[unary.Operand.ID]);
                    }
                    else if (e is ELBinaryOperation binary)
                    {
                        if(binary.Operation == BinaryOperationType.MOV)
                        {
                            f.AddOperation(
                                Assembler.MOV,
                                expr2operand[binary.Left.ID],
                                expr2operand[binary.Right.ID]);
                        }
                        else
                        {
                            expr2operand[e.ID] = f.AddLocal(t is ELStructType, t is ELAtomType a0 && a0.Signed, t.Size, t);
                            if(binary.Left.Type is ELPointerType pt)
                            {
                                AsmOperand right;
                                if(pt.Size == 1)
                                {
                                    right = expr2operand[binary.Right.ID];
                                }
                                else
                                {
                                    var sizeconst = assembler.AddConst(pt.Size, false, PtrSize, ELType.UInt64);
                                    right = f.AddLocal(false, false, PtrSize, ELType.UInt64);
                                    f.AddOperation(
                                        Assembler.MUL,
                                        right,
                                        expr2operand[binary.Right.ID],
                                        sizeconst);
                                }
                                f.AddOperation(
                                    binary.Operation switch
                                    {
                                        BinaryOperationType.ADD => Assembler.ADD,
                                        BinaryOperationType.SUB => Assembler.SUB,
                                        _ => throw new NotImplementedException()
                                    },
                                    expr2operand[e.ID],
                                    expr2operand[binary.Left.ID],
                                    right);
                            }
                            else
                            {
                                f.AddOperation(
                                    binary.Operation switch
                                    {
                                        BinaryOperationType.ADD => Assembler.ADD,
                                        BinaryOperationType.SUB => Assembler.SUB,
                                        BinaryOperationType.MUL => Assembler.MUL,
                                        BinaryOperationType.DIV => Assembler.DIV,
                                        _ => throw new NotImplementedException()
                                    },
                                    expr2operand[e.ID],
                                    expr2operand[binary.Left.ID],
                                    expr2operand[binary.Right.ID]);
                            }
                        }
                    }
                    else throw new NotImplementedException();
                }
            }

            usedLabels.Sort();
            usedLabels.Reverse();
            int[] currIP = Array.ConvertAll(asmf, f => f.GetIP());
            int currExpr = exprCount;
            foreach(var (address, id, context) in usedLabels)
            {
                while(currExpr > address)
                {
                    currExpr--;
                    if(expr2context[currExpr] >= 0)
                        currIP[expr2context[currExpr]] = expr2ip[currExpr];
                }
                assembler.ReplaceConst(label2operand[id], currIP[context]);
            }

            assembler.BuildAndSave(filename, asmf.Where(f => f is not null));
        }
    }
}
