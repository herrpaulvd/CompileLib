using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.PEGen
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe internal struct IMAGE_NT_HEADERS
    {
        public fixed byte Signature[4];
        public IMAGE_FILE_HEADER FileHeader;
        public IMAGE_OPTIONAL_HEADER OptionalHeader;

        public IMAGE_NT_HEADERS(IMAGE_FILE_HEADER fileHeader, IMAGE_OPTIONAL_HEADER optionalHeader) : this()
        {
            Signature[0] = (byte)'P';
            Signature[1] = (byte)'E';
            FileHeader = fileHeader;
            OptionalHeader = optionalHeader;
        }
    }
}
