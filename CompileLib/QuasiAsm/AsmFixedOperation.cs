using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.QuasiAsm
{
    internal class AsmFixedOperation : IAsmOperationType
    {
        public AsmSenderRequired SenderRequired { get; private set; }

        public AsmFixedOperation(AsmSenderRequired senderRequired)
        {
            SenderRequired = senderRequired;
        }
    }
}
