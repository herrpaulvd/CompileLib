using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Common
{
    /// <summary>
    /// Extension methods
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// The method increases the value unless it's divisble by align
        /// </summary>
        /// <param name="value"></param>
        /// <param name="align"></param>
        /// <returns></returns>
        public static int Align(this int value, int align)
        {
            int mod = value % align;
            if (mod != 0) value += align;
            return value - mod;
        }

        /// <summary>
        /// String to HelperTag convertation method
        /// </summary>
        /// <typeparam name="HelperTag"></typeparam>
        /// <param name="self"></param>
        /// <returns></returns>
        public static Alternation<string, HelperTag> ToTag<HelperTag>(this string self)
            where HelperTag : class
        {
            return new(self);
        }

        /// <summary>
        /// Alternative for Activator.CreateInstance to use non-public constructors
        /// </summary>
        /// <param name="t">Class which instance will be created</param>
        /// <param name="parameters">Constructor parameters</param>
        /// <returns></returns>
        public static object? InternalCreateInstance(this Type t, params object[] parameters)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            return Activator.CreateInstance(t, flags, null, parameters, CultureInfo.InvariantCulture);
        }
    }
}
