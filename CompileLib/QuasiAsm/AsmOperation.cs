using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.QuasiAsm
{
    internal struct AsmOperation
    {
        public AsmOperand Destination;
        public object Source;
        public IAsmOperationType OperationType;

        public AsmOperation(AsmOperand destination, object source, IAsmOperationType operationType)
        {
            Destination = destination;
            Source = source;
            OperationType = operationType;
        }
    }
}
