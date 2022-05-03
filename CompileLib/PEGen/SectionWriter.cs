using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.PEGen
{
    unsafe internal class SectionWriter
    {
        private List<byte> buffer = new();
        private int pointer = 0;
        private int maxpointer = 0;

        private int fileAlign, virtualAlign;

        public SectionWriter(int fileAlign, int virtualAlign)
        {
            this.fileAlign = fileAlign;
            this.virtualAlign = virtualAlign;
        }

        public void WriteByte(byte b)
        {
            while (buffer.Count <= pointer) buffer.Add(0);
            buffer[pointer] = b;
            maxpointer = Math.Max(maxpointer, ++pointer);
        }

        public void WriteStruct(void* src, int size, int count = 1)
        {
            var b = (byte*)src;
            count *= size;
            for (int i = 0; i < count; i++)
                WriteByte(b[i]);
        }

        public void WriteString(string s)
        {
            foreach (var c in s)
                WriteByte((byte)c);
            WriteByte(0);
        }

        public void WriteByteSequence(IEnumerable<byte> bytes)
        {
            foreach(var b in bytes) WriteByte(b);
        }

        public void ReserveStruct(int size, int count = 1)
        {
            pointer += count * size;
            maxpointer = Math.Max(maxpointer, pointer);
        }

        private static int Align(int value, int align)
        {
            if (value % align > 0) value += align;
            return value - value % align;
        }

        public int FileSize => Align(maxpointer, fileAlign);
        public int VirtualSize => Align(maxpointer, virtualAlign);

        public byte[] Build() => buffer.Concat(Enumerable.Repeat((byte)0, FileSize - buffer.Count)).ToArray();

        public void SetPointer(int pos)
        {
            pointer = pos;
            maxpointer = Math.Max(maxpointer, pos);
        }

        public int GetPointer() => pointer;
    }
}
