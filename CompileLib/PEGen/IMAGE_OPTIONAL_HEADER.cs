using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.PEGen
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe internal struct IMAGE_OPTIONAL_HEADER
    {
        public const int IMAGE_NUMBEROF_DIRECTORY_ENTRIES = 16;

        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public ulong ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public ulong SizeOfStackReserve;
        public ulong SizeOfStackCommit;
        public ulong SizeOfHeapReserve;
        public ulong SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        private fixed ulong _DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES]; // 8 bytes per struct

        public IMAGE_DATA_DIRECTORY* DataDirectory
        {
            get
            {
                fixed (ulong* ptr = _DataDirectory)
                {
                    return (IMAGE_DATA_DIRECTORY*)ptr;
                }
            }
        }

        public static IMAGE_OPTIONAL_HEADER CreateUseful(
            uint addressOfEntryPoint, 
            ulong imageBase,
            uint sectionAlignment, // 4096
            uint fileAlignment, // 512
            uint sizeOfImage, // in memory
            uint sizeOfHeaders, // = fileAlignment
            ushort subsystem // e.g. 3 is CUI, 2 is GUI
            )
        {
            IMAGE_OPTIONAL_HEADER result = new();
            result.Magic = 0x20b; // x64
            result.AddressOfEntryPoint = addressOfEntryPoint;
            result.ImageBase = imageBase;
            result.SectionAlignment = sectionAlignment;
            result.FileAlignment = fileAlignment;
            result.MajorSubsystemVersion = 4; // winnt
            result.SizeOfImage = sizeOfImage;
            result.SizeOfHeaders = sizeOfHeaders;
            result.Subsystem = subsystem;
            result.NumberOfRvaAndSizes = IMAGE_NUMBEROF_DIRECTORY_ENTRIES;
            return result;
        }
    }
}
