using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public class ELDataBuilder
    {
        private List<byte> data = new();

        internal byte[] CreateArray() => data.ToArray();

        public unsafe void AddRange(void* buffer, int count)
        {
            var a = (byte*)buffer;
            for(int i = 0; i < count; i++) data.Add(a[i]);
        }

        public void Add(byte b) => data.Add(b);
        public void Add(sbyte b) => data.Add((byte)b);
        public void Add(short s) { unsafe { AddRange(&s, 2); } }
        public void Add(char s) { unsafe { AddRange(&s, 2); } }
        public void Add(ushort s) { unsafe { AddRange(&s, 2); } }
        public void Add(int s) { unsafe { AddRange(&s, 4); } }
        public void Add(uint s) { unsafe { AddRange(&s, 4); } }
        public void Add(long s) { unsafe { AddRange(&s, 8); } }
        public void Add(ulong s) { unsafe { AddRange(&s, 8); } }

        public void AddUnicodeString(string s) { foreach (var c in s) Add(c); }

        public void Clear() => data.Clear();
    }
}
