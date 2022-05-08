using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.QuasiAsm
{
    internal class AsmImportCall : IAsmOperationType
    {
        public string Dll { get; private set; }
        public string Name { get; private set; }

        public AsmSenderRequired SenderRequired => AsmSenderRequired.Array;

        public AsmImportCall(string dll, string name)
        {
            Dll = dll;
            Name = name;
        }
    }
}
