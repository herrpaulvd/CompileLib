using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.PEGen
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe internal struct IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;

        public static IMAGE_FILE_HEADER CreateUseful(int numberOfSections)
        {
            IMAGE_FILE_HEADER result = new();
            result.Machine = 0x8664; // AMD 64 arch
            result.NumberOfSections = (ushort)numberOfSections;
            result.SizeOfOptionalHeader = (ushort)sizeof(IMAGE_OPTIONAL_HEADER);
            result.Characteristics = 0x0022; // is executable + can alloc > 2 gb
            return result;
        }
    }
}
