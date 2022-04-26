using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Common
{
    internal class ListSliceGen<T> where T : IComparable<T>
    {
        private List<T> list = new();
        private int startSlice = 0;

        public void Add(T e)
        {
            list.Add(e);
        }

        public void NewStart()
        {
            startSlice = list.Count;
        }

        public ListSlice<T> CreateSlice()
        {
            return new(list, startSlice, list.Count - startSlice);
        }
    }
}
