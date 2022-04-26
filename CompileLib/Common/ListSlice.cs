using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Common
{
    internal struct ListSlice<T> : IComparable<ListSlice<T>> where T : IComparable<T>
    {
        private List<T> list;
        private int start;
        private int length;

        public ListSlice(List<T> list, int start, int length)
        {
            this.list = list;
            this.start = start;
            this.length = length;
        }

        public T this[int index] => list[start + index];
        public int Length => length;

        private ListSlice<T> Skip() => new(list, start + 1, length - 1);

        private static int RecCompare(ListSlice<T> a, ListSlice<T> b)
        {
            int result = 0;
            while(result == 0 && a.Length > 0 && b.Length > 0)
            {
                result = a[0].CompareTo(b[0]);
                a = a.Skip();
                b = b.Skip();
            }
            return result;
        }

        public int CompareTo(ListSlice<T> other)
        {
            return RecCompare(this, other);
        }
    }
}
