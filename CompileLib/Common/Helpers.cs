using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Common
{
    internal static class Helpers
    {
        public static int Align(this int value, int align)
        {
            int mod = value % align;
            if (mod != 0) value += align;
            return value - mod;
        }
    }
}
