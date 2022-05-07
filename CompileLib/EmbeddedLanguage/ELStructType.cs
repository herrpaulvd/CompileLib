using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Common;

namespace CompileLib.EmbeddedLanguage
{
    public class ELStructType : ELType
    {
        private ELType[] fields;
        private int align;
        private int size;

        public int FieldCount => fields.Length;
        public ELType GetFieldType(int index) => fields[index];

        public override bool Equals(object? obj)
            => ReferenceEquals(obj, this);

        public override int GetHashCode()
        {
            return HashCode.Combine(align, fields);
        }

        public override bool IsAssignableTo(ELType type)
        {
            return Equals(type);
        }

        public override string ToString()
        {
            var all = string.Join(", ", fields.Select(f => f.ToString()));
            return $"Struct<{all}>";
        }

        public int Align => align;
        public override int Size => size;

        public ELStructType(int align, params ELType[] fields)
        {
            this.align = align;
            this.fields = fields.ToArray();
            size = fields.Select(f => f.Size.Align(align)).Sum();
        }
    }
}
