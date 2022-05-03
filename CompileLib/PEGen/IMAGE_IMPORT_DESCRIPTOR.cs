using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.PEGen
{
    // TODO: PEBuilder, в т.ч. написание функции формирования таблицы импорта
    // и в принципе секций импорта, данных, кода
    [StructLayout(LayoutKind.Sequential)]
    unsafe internal struct IMAGE_IMPORT_DESCRIPTOR
    {
        public uint OriginalFirstThunk;
        public uint TimeDateStamp;
        public uint ForwarderChain;
        public uint Name;
        public uint FirstThunk;

        [Obsolete]
        public static IMAGE_IMPORT_DESCRIPTOR CreateUseful(
            uint rvaOriginalThunkArray,
            uint rvaName,
            uint rvaThunkArray
            )
        {
            IMAGE_IMPORT_DESCRIPTOR result = new();
            result.OriginalFirstThunk = rvaOriginalThunkArray;
            result.Name = rvaName;
            result.FirstThunk = rvaThunkArray;
            return result;
        }
    }
}
