using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.QuasiAsm
{
    internal enum AsmSenderRequired
    {
        Single,
        Pair,
        Array
    }

    internal interface IAsmOperationType
    {
        AsmSenderRequired SenderRequired { get; }
    }
}
