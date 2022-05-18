using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.PEGen;

namespace CompileLib.QuasiAsm
{
    internal class Assembler
    {
        public const int PtrSize = 8;

        public static readonly AsmFixedOperation RET = new(AsmSenderRequired.None, "RET");
        public static readonly AsmFixedOperation GOTO = new(AsmSenderRequired.Single, "GOTO"); // label
        public static readonly AsmFixedOperation RETVAL = new(AsmSenderRequired.Single, "RETVAL");
        public static readonly AsmFixedOperation MOV = new(AsmSenderRequired.Single, "MOV");
        public static readonly AsmFixedOperation BOOLEAN_NOT = new(AsmSenderRequired.Single, "BOOLNOT");
        public static readonly AsmFixedOperation BITWISE_NOT = new(AsmSenderRequired.Single, "BITWNOT");
        public static readonly AsmFixedOperation NEG = new(AsmSenderRequired.Single, "NEG");
        public static readonly AsmFixedOperation ADD = new(AsmSenderRequired.Pair, "ADD");
        public static readonly AsmFixedOperation SUB = new(AsmSenderRequired.Pair, "SUB");
        public static readonly AsmFixedOperation MUL = new(AsmSenderRequired.Pair, "MUL");
        public static readonly AsmFixedOperation DIV = new(AsmSenderRequired.Pair, "DIV");
        public static readonly AsmFixedOperation LESS = new(AsmSenderRequired.Pair, "LESS");
        public static readonly AsmFixedOperation GOTOIF = new(AsmSenderRequired.Pair, "GOTOIF"); // condition + label
        // label is const AsmOperand, is the number to where go

        private List<AsmOperand> globals = new();
        private List<long> consts = new();
        private List<byte[]> initData = new();

        public AsmOperand AddGlobal(bool struc, bool signed, int size, object tag)
        {
            int id = globals.Count;
            AsmOperand result = new(AsmOperandType.GlobalVar, AsmOperandUse.Val, struc, signed, id, size, tag);
            globals.Add(result);
            return result;
        }

        public AsmOperand AddConst(long value, bool signed, int size, object tag)
        {
            int id = consts.Count;
            consts.Add(value);
            return new(AsmOperandType.Const, AsmOperandUse.Val, false, signed, id, size, tag);
        }

        public void ReplaceConst(AsmOperand operand, long value)
        {
            consts[operand.ID] = value;
        }

        // returns ptr as val, not &val
        public AsmOperand AddInitData(byte[] data, object tag)
        {
            int id = initData.Count;
            initData.Add(data);
            return new AsmOperand(AsmOperandType.InitData, AsmOperandUse.Val, false, false, id, PtrSize, tag);
        }

        public void BuildAndSave(string filename, AsmFunction[] functions)
        {
            for (int i = 0; i < functions.Length; i++)
                functions[i].CallIndex = i;

            List<byte> output = new();
            List<ImportLableTableRecord> importTable = new();
            List<LableTableRecord> dataTable = new();
            List<LableTableRecord> globalVarTable = new();
            List<LableTableRecord> callTable = new();
            List<LableTableRecord> jumpTable = new();

            // wrapper for functions[0]
            output.Add(0xE8);
            for(int i = 0; i < 4; i++) output.Add(0);
            callTable.Add(new(output.Count, 0, true, 4));
            output.AddRange(new byte[]
            {
                0x48, 0x83, 0xEC, 0x20, // sub RSP, 32
                0x40, 0x80, 0xE4, 0xF0, // and SPL, 0xF0
                0x48, 0x31, 0xC9        // xor RCX, RCX
            });
            output.Add(0xFF);
            output.Add(0x14);
            output.Add(0x25);
            for (int i = 0; i < 4; i++) output.Add(0);
            importTable.Add(new(output.Count, new("kernel32.dll", "ExitProcess"), false, 4));

            int[] fun2address = new int[functions.Length];
            foreach (var f in functions)
            {
                fun2address[f.CallIndex] = output.Count;
                f.Compile(output, importTable, dataTable, globalVarTable, callTable, jumpTable, consts);
            }

            // TODO: address resolving
            List<byte> data = new();
            int[] data2address = new int[initData.Count];
            for (int i = 0; i < initData.Count; i++)
            {
                data2address[i] = data.Count;
                data.AddRange(initData[i]);
            }
            int[] global2address = new int[globals.Count];
            for(int i = 0; i < globals.Count; i++)
            {
                global2address[i] = data.Count;
                for (int j = 0; j < globals[i].Size; j++) data.Add(0);
            }

            for(int i = 0; i < dataTable.Count; i++)
            {
                var d = dataTable[i];
                d.What.Offset = data2address[d.What.Offset];
                dataTable[i] = d;
            }
            for(int i = 0; i < globalVarTable.Count; i++)
            {
                var d = globalVarTable[i];
                d.What.Offset = global2address[d.What.Offset];
                dataTable.Add(d);
            }
            for(int i = 0; i < callTable.Count; i++)
            {
                var d = callTable[i];
                d.What.Offset = fun2address[d.What.Offset];
                jumpTable.Add(d);
            }

            PEBuilder.CreateAndSaveImage(
                filename, 
                data, 
                output, 
                importTable.ToArray(), 
                dataTable.ToArray(), 
                jumpTable.ToArray());
        }
    }
}
