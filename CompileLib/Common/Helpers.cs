using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Common
{
    /// <summary>
    /// Extension methods
    /// </summary>
    internal static class Helpers
    {
        public static int Align(this int value, int align)
        {
            int mod = value % align;
            if (mod != 0) value += align;
            return value - mod;
        }

        public static Alternation<string, HelperTag> ToTag<HelperTag>(this string self)
            where HelperTag : class
        {
            return new(self);
        }
    }
}
