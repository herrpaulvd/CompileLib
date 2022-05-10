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
            => AddOperation(type, destination, source);
        public void AddOperation(IAsmOperationType type, AsmOperand destination, AsmOperand left, AsmOperand right)
            => AddOperation(type, destination, Tuple.Create(left, right));
        public void AddOperation(IAsmOperationType type, AsmOperand destination, AsmOperand[] source)
            => AddOperation(type, destination, source);

        public void Compile(
            List<byte> output, 
            List<ImportLableTableRecord> importTable, // abs only
            List<LableTableRecord> dataTable, // abs only
            List<LableTableRecord> globalVarTable, // abs only
            List<LableTableRecord> codeTable, // rel => label inside the func; abs => index of callee, will be conv into rel
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

                // ENTER
                if (allocated > ushort.MaxValue)
                    throw new Exception("The allocated stack size is too long. Try to reduce the variable count or their sizes");
                writeb(0xC8); // enter opcode
                writeptr(&allocated, 2); // stack frame
                writeb(0); // nested stack frame
                
                // emit itself
                // mov RAX, value
                void movRV(AsmOperand operand)
                {
                    if (operand.IsGlobalVar())
                    {
                        if(operand.IsRef())
                        { // TODO: add less sizes + check signed if notwendig
                            writeb(0x48);
                            writeb(0xB8);
                            writell(maxsize); // mov RAX, n
                            globalVarTable.Add(new(output.Count, operand.ID, false, maxsize));
                        }
                        else if(operand.IsVal())
                        {
                            writeb(0x48);
                            writeb(0xA1);
                            writell(maxsize); // mov RAX, [n]
                            globalVarTable.Add(new(output.Count, operand.ID, false, maxsize));
                        }
                        else
                        {
                            writeb(0x48);
                            writeb(0xA1);
                            writell(maxsize); // mov RAX, [n]
                            globalVarTable.Add(new(output.Count, operand.ID, false, maxsize));
                            writearr(0x48, 0x8B, 0x00); // mov RAX, [RAX]
                        }
                    }
                    else if (operand.IsParam() || operand.IsLocalVar())
                    { // TODO, и не забыть вставить выше и ниже MOVZX / MOVSX
                        int cell = operand.IsParam() ? param2cellOffset + maxsize * operand.ID : var2cell[operand.ID];
                        if (operand.IsRef())
                        {

                        }
                        else if (operand.IsVal())
                        {
                            writeb(0x48);
                            writeb(0xA1);
                            writell(maxsize); // mov RAX, [n]
                            globalVarTable.Add(new(output.Count, operand.ID, false, maxsize));
                        }
                        else
                        {
                            writeb(0x48);
                            writeb(0xA1);
                            writell(maxsize); // mov RAX, [n]
                            globalVarTable.Add(new(output.Count, operand.ID, false, maxsize));
                            writearr(0x48, 0x8B, 0x00); // mov RAX, [RAX]
                        }
                    }
                    else if (operand.IsConst())
                    {

                    }
                    else if (operand.IsInitData())
                    {

                    }
                    else throw new NotImplementedException();
                }
                // mov value, RAX
                void movVR(AsmOperand operand)
                {
                    // TODO
                }
                // TODO потом их сделать для RBX
                // struc1 = struc2

                foreach(var op in operations)
                {

                }
            }



            // 4: проверка на ret в конце и добавление если нету
        }
    }
}
