using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.QuasiAsm
{
    internal enum AsmOperandType
    {
        GlobalVar = 0,
        Param = GlobalVar + 1,
        LocalVar = Param + 1,
        Const = LocalVar + 1,
        Nothing = Const,
        InitData = Const + 1,
    }

    internal enum AsmOperandUse
    {
        Ref = AsmOperandType.InitData + 1, // &v
        Val = Ref + 1, // v
        Deref = Val + 1 // *v
    }

    internal struct AsmOperand
    {
        [Flags]
        private enum Flags
        {
            GlobalVar = 1 << AsmOperandType.GlobalVar,
            Param = 1 << AsmOperandType.Param,
            LocalVar = 1 << AsmOperandType.LocalVar,
            Const = 1 << AsmOperandType.Const,
            Nothing = Const,
            InitData = 1 << AsmOperandType.InitData,
            Ref = 1 << AsmOperandUse.Ref,
            Val = 1 << AsmOperandUse.Val,
            Deref = 1 << AsmOperandUse.Deref,
            Struc = Deref << 1,
            Signed = Struc << 1,
            FullExceptType = Ref | Val | Deref | Struc | Signed,
            FullExceptUse = GlobalVar | Param | LocalVar | Const | InitData | Struc | Signed,
        }

        private Flags flags;
        public int ID;
        public int Size;

        private AsmOperand(Flags flags, int id, int size)
        {
            this.flags = flags;
            ID = id;
            Size = size;
        }

        public AsmOperand(
            AsmOperandType operandType, 
            AsmOperandUse operandUse, 
            bool struc,
            bool signed,
            int id, 
            int size)
        {
            ID = id;
            Size = size;

            flags = 0;
            flags |= operandType switch
            {
                AsmOperandType.GlobalVar => Flags.GlobalVar,
                AsmOperandType.Param => Flags.Param,
                AsmOperandType.LocalVar => Flags.LocalVar,
                AsmOperandType.Const => Flags.Const,
                AsmOperandType.InitData => Flags.InitData,
                _ => throw new NotImplementedException(),
            };
            flags |= operandUse switch
            {
                AsmOperandUse.Ref => Flags.Ref,
                AsmOperandUse.Val => Flags.Val,
                AsmOperandUse.Deref => Flags.Deref,
                _ => throw new NotImplementedException()
            };
            if(struc) flags |= Flags.Struc;
            if(signed) flags |= Flags.Signed;
        }

        public bool IsGlobalVar() => (flags & Flags.GlobalVar) != 0;
        public bool IsParam() => (flags & Flags.Param) != 0;
        public bool IsLocalVar() => (flags & Flags.LocalVar) != 0;
        public bool IsConst() => (flags & Flags.Const) != 0;
        public bool IsNothing() => (flags & Flags.Nothing) != 0;
        public bool IsInitData() => (flags & Flags.InitData) != 0;
        public bool IsRef() => (flags & Flags.Ref) != 0;
        public bool IsVal() => (flags & Flags.Val) != 0;
        public bool IsDeref() => (flags & Flags.Deref) != 0;
        public bool IsStruc() => (flags & Flags.Struc) != 0;
        public bool IsSigned() => (flags & Flags.Signed) != 0;
        public bool IsUndefined() => flags == 0;

        public AsmOperand WithType(AsmOperandType t) => new(flags & (Flags.FullExceptType | (Flags)(1 << (int)t)), ID, Size);
        public AsmOperand WithUse(AsmOperandUse u) => new(flags & (Flags.FullExceptUse | (Flags)(1 << (int)u)), ID, Size);
        public AsmOperand SwitchStruc() => new(flags ^ Flags.Struc, ID, Size);
        public AsmOperand SwitchSigned() => new(flags ^ Flags.Signed, ID, Size);
        public AsmOperand WithID(int id) => new(flags, id, Size);
        public AsmOperand WithSize(int size) => new(flags, ID, size);
    }
}
