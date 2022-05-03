using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.PEGen
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe internal struct IMAGE_SECTION_HEADER
    {
        public const int IMAGE_SIZEOF_SHORT_NAME = 8;

        public fixed byte Name[IMAGE_SIZEOF_SHORT_NAME];
        public uint VirtualSize;
        public uint VirtualAddress;
        public uint SizeOfRawData;
        public uint PointerToRawData;
        public uint PointerToRelocations;
        public uint PointerToLinenumbers;
        public ushort NumberOfRelocations;
        public ushort NumberOfLinenumbers;
        public uint Characteristics;

        public static IMAGE_SECTION_HEADER CreateUseful(
            string name,
            uint virtualSize,
            uint virtualAddress,
            uint sizeInFile,
            uint offsetInFile,
            uint characteristics
            )
        {
            IMAGE_SECTION_HEADER result = new();
            int nameLength = Math.Min(name.Length, IMAGE_SIZEOF_SHORT_NAME);
            for (int i = 0; i < nameLength; i++)
                result.Name[i] = (byte)name[i];

            result.VirtualSize = virtualSize;
            result.VirtualAddress = virtualAddress;
            result.SizeOfRawData = sizeInFile;
            result.PointerToRawData = offsetInFile;
            result.Characteristics = characteristics;
            return result;
        }
    }
}
