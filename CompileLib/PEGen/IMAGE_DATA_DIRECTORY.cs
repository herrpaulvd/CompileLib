using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.PEGen
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe internal struct IMAGE_DATA_DIRECTORY
    {
        public uint VirtualAddress; // RVA
        public uint Size; // size in bytes

        public IMAGE_DATA_DIRECTORY(uint virtualAddress, uint size)
        {
            VirtualAddress = virtualAddress;
            Size = size;
        }
    }
}
