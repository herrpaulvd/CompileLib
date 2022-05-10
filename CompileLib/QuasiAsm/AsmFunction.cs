﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}