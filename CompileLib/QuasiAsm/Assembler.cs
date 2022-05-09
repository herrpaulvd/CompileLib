﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.QuasiAsm
{
    internal class Assembler
    {
        public const int PtrSize = 8;

        public static readonly AsmFixedOperation GOTO = new(AsmSenderRequired.Single); // label
        public static readonly AsmFixedOperation RET = new(AsmSenderRequired.Single);
        public static readonly AsmFixedOperation MOV = new(AsmSenderRequired.Single);
        public static readonly AsmFixedOperation BOOLEAN_NOT = new(AsmSenderRequired.Single);
        public static readonly AsmFixedOperation BITWISE_NOT = new(AsmSenderRequired.Single);
        public static readonly AsmFixedOperation NEG = new(AsmSenderRequired.Single);
        public static readonly AsmFixedOperation ADD = new(AsmSenderRequired.Pair);
        public static readonly AsmFixedOperation SUB = new(AsmSenderRequired.Pair);
        public static readonly AsmFixedOperation MUL = new(AsmSenderRequired.Pair);
        public static readonly AsmFixedOperation DIV = new(AsmSenderRequired.Pair);
        public static readonly AsmFixedOperation GOTOIF = new(AsmSenderRequired.Pair); // condition + label
        // label is const AsmOperand, is the number to where go

        private List<AsmOperand> globals = new();
        private List<long> consts = new();
        private List<AsmFunction> functions = new();
        private List<byte[]> initData = new();

        public AsmOperand AddGlobal(bool struc, bool signed, int size)
        {
            int id = globals.Count;
            AsmOperand result = new(AsmOperandType.Param, AsmOperandUse.Val, struc, signed, id, size);
            globals.Add(result);
            return result;
        }

        public AsmOperand AddConst(long value, bool signed, int size)
        {
            int id = consts.Count;
            consts.Add(value);
            return new(AsmOperandType.Param, AsmOperandUse.Val, false, signed, id, size);
        }

        public void ReplaceConst(AsmOperand operand, long value)
        {
            consts[operand.ID] = value;
        }

        public void AddFunction(AsmFunction function)
            => functions.Add(function);

        // returns ptr as val, not &val
        public AsmOperand AddInitData(byte[] data)
        {
            int id = initData.Count;
            initData.Add(data);
            return new AsmOperand(AsmOperandType.Param, AsmOperandUse.Val, false, false, id, PtrSize);
        }
    }
}
