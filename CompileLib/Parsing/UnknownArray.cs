using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Array of objects with unknown types
    /// </summary>
    internal class UnknownArray
    {
        private readonly object?[] children;

        public UnknownArray(int length)
        {
            children = new object?[length];
        }

        public object? this[int index]
        {
            get => children[index];
            set => children[index] = value;
        }

        private static bool IsAssignable(Type src, Type dest)
        {
            if (src == typeof(Common.Token))
                return typeof(Parsing.Token).IsAssignableTo(dest) || typeof(string).IsAssignableTo(dest);

            return src.IsAssignableTo(dest);
        }

        private static object? Convert(object? o, Type dest, Func<int, string> typeToStr)
        {
            if (o is Common.Token tk)
            {
                if (typeof(Parsing.Token).IsAssignableTo(dest))
                    return new Parsing.Token(typeToStr(tk.Type), tk.Self, tk.Line, tk.Column);
                if (typeof(string).IsAssignableTo(dest))
                    return tk.Self;
                Debug.Fail("Check before was wrong");
            }
            return o;
        }

        public bool Check(Type t)
        {
            return children.All(x => x is null || IsAssignable(x.GetType(), t));
        }

        public object? Cast(Type t, Func<int, string> typeToStr)
        {
            var result = Array.CreateInstance(t, children.Length);
            for (int i = 0; i < children.Length; i++)
                result.SetValue(Convert(children[i], t, typeToStr), i);
            return result;
        }
    }
}
