using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.PEGen
{
    unsafe internal class FileWriter
    {
        private List<SectionWriter> sections = new();

        private int fileAlign, virtualAlign;

        public FileWriter(int fileAlign, int virtualAlign)
        {
            this.fileAlign = fileAlign;
            this.virtualAlign = virtualAlign;
        }

        public SectionWriter AllocSection()
        {
            SectionWriter result = new(fileAlign, virtualAlign);
            sections.Add(result);
            return result;
        }

        public SectionWriter this[int index] => sections[index];
        public int FileSize => sections.Select(s => s.FileSize).Sum();
        public int VirtualSize => sections.Select(s => s.VirtualSize).Sum();

        public byte[] Build()
        {
            byte[] result = new byte[FileSize];
            int ptr = 0;
            foreach(var s in sections)
                foreach(var b in s.Build())
                    result[ptr++] = b;
            return result;
        }

        public void WriteFile(string filename)
        {
            File.WriteAllBytes(filename, Build());
        }
    }
}
