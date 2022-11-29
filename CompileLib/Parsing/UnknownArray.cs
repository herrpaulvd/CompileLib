using CompileLib.ParserTools;
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
        private readonly AnyParsed[] children;

        public UnknownArray(int length)
        {
            children = new AnyParsed[length];
        }

        public AnyParsed this[int index]
        {
            get => children[index];
            set => children[index] = value;
        }

        public Array? Cast(Type t, Func<AnyParsed, object?> convertion)
        {
            var result = Array.CreateInstance(t, children.Length);
            for (int i = 0; i < children.Length; i++)
                result.SetValue(convertion(children[i]), i);
            return result;
        }
    }
}
