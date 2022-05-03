using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.PEGen
{
    /* 
     * Sequence of structs:
     * IMAGE_DOS_HEADER
     * IMAGE_NT_HEADERS
     * array of IMAGE_SECTION_HEADER
     * <END OF SECTOR>
     * array of sections itself
     */

    [StructLayout(LayoutKind.Sequential)]
    unsafe internal struct IMAGE_DOS_HEADER
    {
        public fixed byte e_magic[2];
        public ushort e_cblp;
        public ushort e_cp;
        public ushort e_crlc;
        public ushort e_cparhdr;
        public ushort e_minalloc;
        public ushort e_maxalloc;
        public ushort e_ss;
        public ushort e_sp;
        public ushort e_csum;
        public ushort e_ip;
        public ushort e_cs;
        public ushort e_lfarlc;
        public ushort e_ovno;
        public fixed ushort e_res[4];
        public ushort e_oemid;
        public ushort e_oeminfo;
        public fixed ushort e_res2[10];
        public uint e_lfanew;

        public static IMAGE_DOS_HEADER CreateUseful()
        {
            IMAGE_DOS_HEADER result = new();
            result.e_magic[0] = (byte)'M';
            result.e_magic[1] = (byte)'Z';
            result.e_lfanew = (uint)sizeof(IMAGE_DOS_HEADER);
            return result;
        }
    }
}
