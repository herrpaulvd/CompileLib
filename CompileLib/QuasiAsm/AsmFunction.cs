using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.PEGen;
using CompileLib.Common;

namespace CompileLib.QuasiAsm
{
    internal class AsmFunction : IAsmOperationType
    {
        internal int CallIndex;
        private List<AsmOperand> parameters = new();
        private AsmOperand result;
        private List<AsmOperand> locals = new();
        private List<AsmOperation> operations = new();

        public AsmSenderRequired SenderRequired => AsmSenderRequired.Array;

        public AsmFunction(bool struc, bool signed, int size, object tag)
        {
            result = new(AsmOperandType.LocalVar, AsmOperandUse.Val, struc, signed, 0, size, tag);
        }

        public AsmOperand AddParameter(bool struc, bool signed, int size, object tag)
        {
            int id = parameters.Count;
            AsmOperand result = new(AsmOperandType.Param, AsmOperandUse.Val, struc, signed, id, size, tag);
            parameters.Add(result);
            return result;
        }

        public AsmOperand AddLocal(bool struc, bool signed, int size, object tag)
        {
            int id = locals.Count;
            AsmOperand result = new(AsmOperandType.LocalVar, AsmOperandUse.Val, struc, signed, id, size, tag);
            locals.Add(result);
            return result;
        }

        public int GetIP() => operations.Count;
        
        private void AddOperation(IAsmOperationType type, AsmOperand destination, object? source)
            => operations.Add(new AsmOperation(destination, source, type));
        public void AddOperation(IAsmOperationType type, AsmOperand destination)
            => AddOperation(type, destination, (object?)null);
        public void AddOperation(IAsmOperationType type, AsmOperand destination, AsmOperand source)
            => AddOperation(type, destination, (object)source);
        public void AddOperation(IAsmOperationType type, AsmOperand destination, AsmOperand left, AsmOperand right)
            => AddOperation(type, destination, Tuple.Create(left, right));
        public void AddOperation(IAsmOperationType type, AsmOperand destination, AsmOperand[] source)
            => AddOperation(type, destination, (object)source);

        public void Compile(
            List<byte> output, 
            List<ImportLableTableRecord> importTable, // abs only, what = import itself
            List<LableTableRecord> dataTable, // abs only, what = initialized data index
            List<LableTableRecord> globalVarTable, // abs only, what = global var index
            List<LableTableRecord> callTable, // rel only, what = function index
            List<LableTableRecord> jumpTable, // rel only, what = address of near jump
            List<long> index2const
            )
        {
            OrderedGraph g = new(operations.Count + 2);
            g.AddEdge(0, 1);
            for(int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                var t = op.OperationType;
                if(t == Assembler.GOTO)
                {
                    var ip = (int)index2const[((AsmOperand)op.Source).ID];
                    g.AddEdge(i + 1, ip);
                    continue;
                }
                if(t == Assembler.GOTOIF)
                {
                    var (_, labelArg) = op.Source as Tuple<AsmOperand, AsmOperand>;
                    var ip = (int)index2const[labelArg.ID];
                    g.AddEdge(i + 1, ip);
                }
                g.AddEdge(i + 1, i + 2);
            }
            g.GetMinMaxArrays(out int[] min, out int[] max);

            // paramMin[i] = min[0] = 0 always
            const int parammin = 0;
            int[] parammax = new int[parameters.Count];
            bool[] paramref = new bool[parameters.Count];

            int[] varmin = new int[locals.Count];
            int[] varmax = new int[locals.Count];
            bool[] varref = new bool[locals.Count];
            Array.Fill(varmin, min.Length);

            for(int i = 0; i < operations.Count; i++)
            {
                int vertex = i + 1;
                var op = operations[i];
                var t = op.OperationType;

                void updateArg(AsmOperand arg)
                {
                    if (arg.IsStruc()) return;
                    if (arg.IsParam())
                    {
                        // TODO раздебагать тут!!!!!!!
                        parammax[arg.ID] = Math.Max(parammax[arg.ID], max[vertex]);
                        if (arg.IsRef())
                            paramref[arg.ID] = true;
                    }
                    if (arg.IsLocalVar())
                    {
                        varmax[arg.ID] = Math.Max(varmax[arg.ID], max[vertex]);
                        if (arg.IsRef())
                            varref[arg.ID] = true;
                    }
                }

                switch(t.SenderRequired)
                {
                    case AsmSenderRequired.None:
                        break;
                    case AsmSenderRequired.Single:
                        updateArg((AsmOperand)op.Source);
                        break;
                    case AsmSenderRequired.Pair:
                        var (arg1, arg2) = op.Source as Tuple<AsmOperand, AsmOperand>;
                        updateArg(arg1);
                        updateArg(arg2);
                        break;
                    case AsmSenderRequired.Array:
                        foreach(var arg in op.Source as AsmOperand[])
                            updateArg(arg);
                        break;
                }

                if (op.Destination.IsLocalVar())
                    varmin[op.Destination.ID] = Math.Min(varmin[op.Destination.ID], min[vertex]);
            }

            List<(int time, int @event, int index)> scanline = new();
            const int EVENT_ENTER = 0;
            const int EVENT_LEAVE = -1;
            for(int i = 0; i < parameters.Count; i++)
            {
                // param struc don't exist
                // parammin always = 0
                if (!paramref[i])
                    scanline.Add((parammax[i] + 1, EVENT_LEAVE, ~i));
            }
            for(int i = 0; i < locals.Count; i++)
            {
                if (locals[i].IsStruc()) continue;
                scanline.Add((varmin[i], EVENT_ENTER, i));
                if(!varref[i])
                    scanline.Add((varmax[i] + 1, EVENT_LEAVE, i));
            }
            scanline.Sort();

            const int maxsize = Assembler.PtrSize;
            const int param2cellOffset = maxsize * 2; // 8 ebp, 8 near ret address
            // passing right to left, so param2cell[i] = offset + maxsize * i
            int[] var2cell = new int[locals.Count];
            Dictionary<int, SortedSet<int>> cells = new();
            for (int i = 1; i <= maxsize; i *= 2)
                cells.Add(i, new());

            // using params rests too
            for(int i = 0; i < parameters.Count; i++)
            {
                int cell = param2cellOffset + maxsize * i;
                for (int s = parameters[i].Size; s < maxsize; s *= 2)
                    cells[s].Add(cell + s);
            }
            
            int allocated = 0;
            foreach(var (time, @event, index) in scanline)
            {
                int reqsize = (index < 0 ? parameters[~index] : locals[index]).Size;
                if (@event == EVENT_ENTER)
                {
                    int foundsize = -1;
                    int foundcell = -1;
                    for (int i = reqsize; i <= maxsize; i *= 2)
                        if (cells[i].Count > 0)
                        {
                            foundsize = i;
                            foundcell = cells[i].Min;
                            cells[i].Remove(foundcell);
                            break;
                        }
                    if (foundsize == -1)
                    {
                        foundsize = maxsize;
                        allocated += maxsize;
                        foundcell = -allocated;
                    }
                    while (foundsize > reqsize)
                    {
                        foundsize /= 2;
                        cells[foundsize].Add(foundcell + foundsize);
                    }
                    var2cell[index] = foundcell;
                }
                else // EVENT_LEAVE
                {
                    int cell = index < 0 ? param2cellOffset + maxsize * (~index) : var2cell[index];
                    while (reqsize < maxsize)
                    {
                        int parentsize = reqsize * 2;
                        int brother = cell % parentsize == 0 ? cell + reqsize : cell - reqsize;
                        if (cells[reqsize].Contains(brother))
                        {
                            cells[reqsize].Remove(brother);
                            reqsize *= 2;
                            cell = Math.Min(cell, brother);
                        }
                        else break;
                    }
                    cells[reqsize].Add(cell);
                }
            }
            // struct locals
            for(int i = 0; i < locals.Count; i++)
                if(locals[i].IsStruc())
                {
                    allocated += locals[i].Size;
                    var2cell[i] = -allocated;
                }
            // align to maxSize
            if (allocated % maxsize > 0) allocated += maxsize - allocated % maxsize;
            
            unsafe
            {
                void writeptr(void* src, int cnt)
                {
                    byte* buff = (byte*)src;
                    for(int i = 0; i < cnt; i++) output.Add(buff[i]);
                }
                void writearr(params byte[] src)
                {
                    for (int i = 0; i < src.Length; i++) output.Add(src[i]);
                }
                void writeb(byte b) => output.Add(b);

                const byte LL = 0xAA;
                void writell(int cnt)
                {
                    for (int i = 0; i < cnt; i++) output.Add(LL);
                }

                const byte REXW = 0x48; // 64bit operand OR sign for MOVSXD
                const byte REXR = 0x44; // ext MODR/M.reg
                const byte REXX = 0x42; // ext SIB.index
                const byte REXB = 0x41; // ext MORR/M.r/m and SIB.base
                const byte REXEMPTY = 0x40;

                const byte MOD00 = 0x00;
                const byte MOD01 = 0x40;
                const byte MOD10 = 0x80;
                const byte MOD11 = 0xC0;

                const byte ID_RAX = 0;
                const byte ID_RCX = 1;
                const byte ID_RDX = 2;
                const byte ID_RBX = 3;
                const byte ID_RSP = 4;
                const byte ID_RBP = 5;
                const byte ID_RSI = 6;
                const byte ID_RDI = 7;
                const byte ID_R8 = 8;
                const byte ID_R9 = 9;
                const byte ID_R15 = 15;

                const byte OP_LEA = 0x8D;
                const byte OP_MOV = 0x8B;
                const byte OP_LONGMOV = 0xB8;
                const byte OP_MOVSXD = 0x63;
                const byte OP_MOVBACK = 0x89;
                const byte OP_MOVBACK8 = 0x88;

                byte OP_MOVSZX(AsmOperand operand)
                {
                    return (byte)(operand.IsSigned()
                           ? (operand.Size == 1 ? 0xBE : 0xBF)
                           : (operand.Size == 1 ? 0xB6 : 0xB7));
                }
                const byte PREF_MOVSZX = 0x0F;
                const byte PREF_16 = 0x66;

                void writeMOVSXDprefix(byte REX, AsmOperand operand)
                {
                    if (!operand.IsSigned())
                        REX = (byte)((REX ^ REXW) | REXEMPTY);
                    if (REX != REXEMPTY)
                        writeb(REX);
                }

                // ENTER
                if (allocated > ushort.MaxValue)
                    throw new Exception("The allocated stack size is too long. Try to reduce the variable count or their sizes");
                writeb(0xC8); // ENTER opcode
                writeptr(&allocated, 2); // stack frame
                writeb(0); // nested stack frame

                void movRR(byte REX2, byte MODRM2, AsmOperand operand)
                {
                    if (operand.Size == maxsize)
                    {
                        writearr(REX2, OP_MOV, MODRM2); // mov RAX, [RAX]
                    }
                    else if (operand.Size == 4)
                    {
                        writeMOVSXDprefix(REX2, operand);
                        writearr(OP_MOVSXD, MODRM2); // movsxd RAX, DWORD PTR [RAX]
                    }
                    else
                    {
                        writearr(REX2, PREF_MOVSZX, OP_MOVSZX(operand), MODRM2); // mov(S|Z)X, (BYTE|WORD) PTR [RAX]
                    }
                }

                // emit itself
                // mov reg, operand (reg not only RAX)
                void movRV(AsmOperand operand, int reg)
                {
                    byte REX1 = REXW; // for single reg
                    if (reg >= ID_R8) REX1 |= REXR;
                    byte REX2 = REX1; // for two regs
                    if (reg >= ID_R8) REX2 |= REXB;

                    byte MODRM0 = (byte)((reg & 0b111) << 3); // temp res
                    byte MODRM1 = (byte)(MODRM0 | 0b100); // for single reg
                    const byte SIB1 = 0x25; // for single reg
                    byte MODRM2 = (byte)(MODRM0 | (reg & 0b111)); // for two regs

                    if (operand.IsGlobalVar())
                    {
                        if (operand.IsRef())
                        {
                            writearr(REX1, OP_LEA, MODRM1, SIB1);
                            writell(4); // lea RAX, [n]
                            globalVarTable.Add(new(output.Count, operand.ID, false, 4));
                        }
                        else if(operand.IsVal())
                        {
                            if(operand.Size == maxsize)
                            {
                                writearr(REX1, OP_MOV, MODRM1, SIB1); // mov RAX, [n]
                            }
                            else if(operand.Size == 4)
                            {
                                writeMOVSXDprefix(REX1, operand);
                                writearr(OP_MOVSXD, MODRM1, SIB1); // movsxd RAX, DWORD PTR [n]
                            }
                            else
                            {
                                writearr(REX1, PREF_MOVSZX, OP_MOVSZX(operand), MODRM1, SIB1); // mov(S|Z)X, (BYTE|DWORD) PTR [n]
                            }
                            writell(4); // PTR [n] itself
                            globalVarTable.Add(new(output.Count, operand.ID, false, 4));
                        }
                        else
                        {
                            writearr(REX1, OP_MOV, MODRM1, SIB1);
                            writell(4); // mov RAX, [n]
                            globalVarTable.Add(new(output.Count, operand.ID, false, 4));
                            movRR(REX2, MODRM2, operand); // mov RAX, [RAX]
                        }
                    }
                    else if (operand.IsParam() || operand.IsLocalVar())
                    {
                        int cell = operand.IsParam() ? param2cellOffset + maxsize * operand.ID : var2cell[operand.ID];
                        byte MODRMBP = (byte)(MODRM0 | 0b101);
                        int offsetSize;
                        if(byte.MinValue <= cell && cell <= byte.MaxValue)
                        {
                            MODRMBP |= MOD01;
                            offsetSize = 1;
                        }
                        else
                        {
                            MODRMBP |= MOD10;
                            offsetSize = 4;
                        }

                        if (operand.IsRef())
                        {
                            writearr(REX1, OP_LEA, MODRMBP);
                            writeptr(&cell, offsetSize); // lea RAX, [RBP + cell]
                        }
                        else if (operand.IsVal())
                        {
                            if (operand.Size == maxsize)
                            {
                                writearr(REX1, OP_MOV, MODRMBP); // mov RAX, [RBP + cell]
                            }
                            else if (operand.Size == 4)
                            {
                                writeMOVSXDprefix(REX1, operand);
                                writearr(OP_MOVSXD, MODRMBP); // movsxd RAX, DWORD PTR [RBP + cell]
                            }
                            else
                            {
                                writearr(REX1, PREF_MOVSZX, OP_MOVSZX(operand), MODRMBP); // mov(S|Z)X, (BYTE|DWORD) PTR [RBP + cell]
                            }
                            writeptr(&cell, offsetSize); // +cell itself
                        }
                        else
                        {
                            writearr(REX1, OP_MOV, MODRMBP); 
                            writeptr(&cell, offsetSize); // mov RAX, [RBP + cell]
                            movRR(REX2, MODRM2, operand); // mov RAX, [RAX]
                        }
                    }
                    else if (operand.IsConst())
                    {
                        byte LONGMOV = (byte)(OP_LONGMOV + (reg & 0b111));
                        long value = index2const[operand.ID];
                        if (operand.IsVal())
                        {
                            writeb(REX1);
                            writeb(LONGMOV);
                            writeptr(&value, maxsize); // mov RAX, const
                        }
                        else if (operand.IsDeref())
                        {
                            writeb(REX1);
                            writeb(LONGMOV);
                            writeptr(&value, maxsize); // mov RAX, const
                            movRR(REX2, MODRM2, operand); // mov RAX, [RAX]
                        }
                        else throw new NotImplementedException(); // cannot get ref on const
                    }
                    else if (operand.IsInitData())
                    {
                        if (operand.IsVal())
                        {
                            writearr(REX1, OP_LEA, MODRM1, SIB1);
                            writell(4); // lea RAX, [n]
                            dataTable.Add(new(output.Count, operand.ID, false, 4));
                        }
                        else if (operand.IsDeref())
                        {
                            if (operand.Size == maxsize)
                            {
                                writearr(REX1, OP_MOV, MODRM1, SIB1); // mov RAX, [n]
                            }
                            else if (operand.Size == 4)
                            {
                                writeMOVSXDprefix(REX1, operand);
                                writearr(OP_MOVSXD, MODRM1, SIB1); // movsxd RAX, DWORD PTR [n]
                            }
                            else
                            {
                                writearr(REX1, PREF_MOVSZX, OP_MOVSZX(operand), MODRM1, SIB1); // mov(S|Z)X, (BYTE|DWORD) PTR [n]
                            }
                            writell(4); // PTR [n] itself
                            dataTable.Add(new(output.Count, operand.ID, false, 4));
                        }
                        else throw new NotImplementedException(); // cannot get ref on const
                    }
                    else throw new NotImplementedException();
                }

                // mov operand, RAX (only, because result reg)
                // uses RBX
                void movVR(AsmOperand operand)
                {
                    if (operand.IsUndefined()) return; // no result is needed
                    if (operand.IsRef()) throw new NotImplementedException(); // ref is a const

                    const byte REX = REXW; // uses no r8-r15 in any case
                    const byte MODRM1 = 0x04; // when mov [addr], RAX
                    const byte SIB1 = 0x25; // SIB for MODRM1
                    const byte MODRM2 = ID_RBX; // when mov [RBX], RAX
                    const byte MODRMLDADDR = MODRM1 | (ID_RBX << 3); // when mov RBX, [addr]
                    const byte SIBLDADDR = SIB1; // SIB for MODRMLDADDR

                    if (operand.IsGlobalVar())
                    {
                        if(operand.IsVal())
                        {
                            if (operand.Size == maxsize) writeb(REX);
                            if (operand.Size == 2) writeb(PREF_16);
                            writearr(operand.Size == 1 ? OP_MOVBACK8 : OP_MOVBACK, MODRM1, SIB1);
                            writell(4); // mov [n], RAX
                            globalVarTable.Add(new(output.Count, operand.ID, false, 4));
                        }
                        else
                        {
                            writearr(REX, OP_MOV, MODRMLDADDR, SIBLDADDR);
                            writell(4); // mov RBX, [n]
                            globalVarTable.Add(new(output.Count, operand.ID, false, 4));
                            if (operand.Size == maxsize) writeb(REX);
                            if (operand.Size == 2) writeb(PREF_16);
                            writearr(operand.Size == 1 ? OP_MOVBACK8 : OP_MOVBACK, MODRM2); // mov [RBX], RAX
                        }
                    }
                    else if (operand.IsParam() || operand.IsLocalVar())
                    {
                        int cell = operand.IsParam() ? param2cellOffset + maxsize * operand.ID : var2cell[operand.ID];
                        byte MODRMBP = 0b101;
                        int offsetSize;
                        if (byte.MinValue <= cell && cell <= byte.MaxValue)
                        {
                            MODRMBP |= MOD01;
                            offsetSize = 1;
                        }
                        else
                        {
                            MODRMBP |= MOD10;
                            offsetSize = 4;
                        }

                        if (operand.IsVal())
                        {
                            if (operand.Size == maxsize) writeb(REX);
                            if (operand.Size == 2) writeb(PREF_16);
                            writearr(operand.Size == 1 ? OP_MOVBACK8 : OP_MOVBACK, MODRMBP);
                            writeptr(&cell, offsetSize); // mov [RBP + cell], RAX
                        }
                        else
                        {
                            MODRMBP |= ID_RBX << 3; // set RBX instead of RAX
                            writearr(REX, OP_MOV, MODRMBP);
                            writeptr(&cell, offsetSize); // mov RBX, [RBP + cell]
                            if (operand.Size == maxsize) writeb(REX);
                            if (operand.Size == 2) writeb(PREF_16);
                            writearr(operand.Size == 1 ? OP_MOVBACK8 : OP_MOVBACK, MODRM2); // mov [RBX], RAX
                        }
                    }
                    else if (operand.IsConst())
                    {
                        long value = index2const[operand.ID];
                        const byte LONGMOV = OP_LONGMOV + (ID_RBX & 0b111);
                        if (operand.IsDeref())
                        {
                            writeb(REX);
                            writeb(LONGMOV);
                            writeptr(&value, maxsize); // mov RBX, const
                            if (operand.Size == maxsize) writeb(REX);
                            if (operand.Size == 2) writeb(PREF_16);
                            writearr(operand.Size == 1 ? OP_MOVBACK8 : OP_MOVBACK, MODRM2); // mov [RBX], RAX
                        }
                        else throw new NotImplementedException();
                    }
                    else if (operand.IsInitData())
                    {
                        if (operand.IsDeref())
                        {
                            if (operand.Size == maxsize) writeb(REX);
                            if (operand.Size == 2) writeb(PREF_16);
                            writearr(operand.Size == 1 ? OP_MOVBACK8 : OP_MOVBACK, MODRM1, SIB1);
                            writell(4); // mov [n], RAX
                            dataTable.Add(new(output.Count, operand.ID, false, 4));
                        }
                        else throw new NotImplementedException();
                    }
                    else throw new NotImplementedException();
                }

                AsmOperand upOperand(AsmOperand operand)
                {
                    if (operand.IsVal())
                    {
                        return operand.ChangeUse(AsmOperandUse.Ref, false, false, maxsize, this);
                    }
                    else if (operand.IsDeref())
                    {
                        return operand.ChangeUse(AsmOperandUse.Val, false, false, maxsize, this);
                    }
                    else throw new NotImplementedException();
                }

                void movStruc(AsmOperand src, AsmOperand dst)
                {
                    int size = src.Size;
                    writearr(0x48, 0xC7, 0xC1);
                    writeptr(&size, 4); // mov RCX, size

                    src = upOperand(src); // src := &src
                    dst = upOperand(dst); // dst := &dst
                    movRV(src, ID_RSI); // mov RSI, src
                    movRV(dst, ID_RDI); // mov RDI, dst
                    writeb(0xF3);
                    writeb(0xA4); // REP MOVSB
                }

                List<int> label2address = new();
                List<LableTableRecord> jumps = new();
                bool returned = false;
                foreach(var op in operations)
                {
                    returned = false;
                    label2address.Add(output.Count);
                    AsmOperand GetSingle() => (AsmOperand)op.Source;
                    Tuple<AsmOperand, AsmOperand> GetPair() => (Tuple<AsmOperand, AsmOperand>)op.Source;
                    AsmOperand[] GetSequence() => (AsmOperand[])op.Source;

                    var t = op.OperationType;
                    if (t == Assembler.RET)
                    {
                        writeb(0xC9); // leave
                        writeb(0xC3); // ret(near)
                        returned = true;
                    }
                    else if (t == Assembler.GOTO)
                    {
                        int label = (int)index2const[GetSingle().ID];
                        writeb(0xE9);
                        writell(4); // jmp(near)
                        jumps.Add(new(output.Count, label, true, 4));
                    }
                    else if (t == Assembler.RETVAL)
                    {
                        movRV(GetSingle(), ID_RAX);
                        writeb(0xC9); // leave
                        writeb(0xC3); // ret(near)
                        returned = true;
                    }
                    else if (t == Assembler.MOV)
                    {
                        movRV(GetSingle(), ID_RAX);
                        movVR(op.Destination);
                    }
                    else if (t == Assembler.BOOLEAN_NOT)
                    {
                        movRV(GetSingle(), ID_RBX);
                        writearr(
                            0x48, 0x31, 0xC0, // xor rax, rax
                            0x48, 0x83, 0xC3, 0xFF, // add rbx, -1 (CF = 1 when rbx != 0)
                            0x48, 0x83, 0xD0, 0xFF // adc rax, -1 (rbx != 0 => CF = 1 => 0; rbx = 0 => CF = 0 => -1)
                            );
                        movVR(op.Destination);
                    }
                    else if (t == Assembler.BITWISE_NOT)
                    {
                        movRV(GetSingle(), ID_RAX);
                        writearr(0x48, 0xF7, 0xD0); // not rax
                        movVR(op.Destination);
                    }
                    else if (t == Assembler.NEG)
                    {
                        movRV(GetSingle(), ID_RAX);
                        writearr(0x48, 0xF7, 0xD8); // neg rax
                        movVR(op.Destination);
                    }
                    else if (t == Assembler.ADD)
                    {
                        var (a, b) = GetPair();
                        movRV(a, ID_RAX);
                        movRV(b, ID_RBX);
                        writearr(0x48, 0x01, 0xD8); // add rax, rbx
                        movVR(op.Destination);
                    }
                    else if (t == Assembler.SUB)
                    {
                        var (a, b) = GetPair();
                        movRV(a, ID_RAX);
                        movRV(b, ID_RBX);
                        writearr(0x48, 0x29, 0xD8); // sub rax, rbx
                        movVR(op.Destination);
                    }
                    else if (t == Assembler.MUL)
                    {
                        var (a, b) = GetPair();
                        if (a.IsSigned() != b.IsSigned()) throw new NotImplementedException();
                        movRV(a, ID_RAX);
                        movRV(b, ID_RBX);
                        if (a.IsSigned())
                        {
                            writearr(0x48, 0xF7, 0xEB); // imul rbx
                        }
                        else
                        {
                            writearr(0x48, 0xF7, 0xE3); // mul rbx
                        }
                        movVR(op.Destination);
                    }
                    else if (t == Assembler.DIV)
                    {// todo
                        var (a, b) = GetPair();
                        if (a.IsSigned() != b.IsSigned()) throw new NotImplementedException();
                        movRV(a, ID_RAX);
                        movRV(b, ID_RBX);
                        if (a.IsSigned())
                        {
                            writearr(
                                0x48, 0x99,      // CQO
                                0x48, 0xF7, 0xFB // idiv rbx
                                );
                        }
                        else
                        {
                            writearr(
                                0x48, 0x31, 0xD2, // xor rdx, rdx
                                0x48, 0xF7, 0xF3  // div rbx
                                );
                        }
                        movVR(op.Destination);
                        // TODO when creating Assembler.MOD: make movVR for RDX
                    }
                    else if (t == Assembler.LESS)
                    {
                        var (a, b) = GetPair();
                        movRV(a, ID_RAX);
                        movRV(b, ID_RBX);

                        if(a.IsSigned() != b.IsSigned()) throw new NotImplementedException();
                        if(a.IsSigned())
                        {
                            writearr(
                                0x48, 0x39, 0xD8, // cmp rax, rbx
                                0x7C, 0x05, // jl (mov rax, 1)
                                0x48, 0x31, 0xC0, // xor rax, rax
                                0xEB, 0x07, // jmp after (mov rax, rax)
                                0x48, 0xC7, 0xC0, 0x01, 0x00, 0x00, 0x00 // mov rax, 1
                                );
                        }
                        else
                        {
                            writearr(
                                   0x48, 0x39, 0xD8, // cmp rax, rbx
                                   0x72, 0x05, // jb (mov rax, 1)
                                   0x48, 0x31, 0xC0, // xor rax, rax
                                   0xEB, 0x07, // jmp after (mov rax, rax)
                                   0x48, 0xC7, 0xC0, 0x01, 0x00, 0x00, 0x00 // mov rax, 1
                                   );
                        }

                        movVR(op.Destination);
                    }
                    else if (t == Assembler.GOTOIF)
                    {
                        var (a, b) = GetPair();
                        int label = (int)index2const[b.ID];
                        movRV(a, ID_RAX);
                        writeb(0x0F);
                        writeb(0x85);
                        writell(4); // jnz label
                        jumps.Add(new(output.Count, label, true, 4));
                    }
                    else if (t is AsmFunction f)
                    {
                        foreach(var arg in GetSequence().Reverse())
                        {
                            movRV(arg, ID_RAX);
                            writeb(0x50); // PUSH RAX
                        }
                        writeb(0xE8);
                        writell(4); // call f
                        callTable.Add(new(output.Count, f.CallIndex, true, 4));
                        movVR(op.Destination);
                    }
                    else if (t is AsmImportCall importf)
                    {
                        var args = GetSequence();
                        int n = args.Length;

                        writearr(
                            0x49, 0x89, 0xE7, // mov R15, RSP
                            0x40, 0x80, 0xE4, 0xF0 // and SPL, 0xF0
                            );
                        if (n > 4 && n % 2 == 1)
                            writearr(0x48, 0x83, 0xEC, 0x08); // sub RSP, 8
                        for(int i = n - 1; i >= 4; i--)
                        {
                            movRV(args[i], ID_RAX);
                            writeb(0x50); // PUSH RAX
                        }
                        if (n > 0) movRV(args[0], ID_RCX);
                        if (n > 1) movRV(args[1], ID_RDX);
                        if (n > 2) movRV(args[2], ID_R8);
                        if (n > 3) movRV(args[3], ID_R9);
                        writearr(0x48, 0x83, 0xEC, 0x20); // sub RSP, 32
                        writearr(0xFF, 0x14, 0x25);
                        writell(4); // call qword ptr [importref]
                        importTable.Add(new(output.Count, new(importf.Dll, importf.Name), false, 4));
                        movVR(op.Destination);
                        writearr(0x4C, 0x89, 0xFC); // mov RSP, R15
                    }
                    else throw new NotImplementedException();
                }

                label2address.Add(output.Count);
                foreach(var _jump in jumps)
                {
                    var jump = _jump;
                    if ((jump.What.Offset = label2address[jump.What.Offset]) == output.Count)
                        returned = false;
                    jumpTable.Add(jump);
                }

                if(!returned)
                {
                    // return 0
                    writearr(
                        0x48, 0x31, 0xC0, // xor rax, rax
                        0xC9, // leave
                        0xC3  // ret
                        );
                }
            }
        }
    }
}
