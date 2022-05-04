using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Semantic
{
    // temp version
    public class Scope
    {
        private SortedDictionary<string, object> codeObjects = new();
        private SortedDictionary<string, object> hiddenObjects = new();

        public bool AddCodeObject(string name, object info)
        {
            if(codeObjects.ContainsKey(name))
                return false;
            codeObjects.Add(name, info);
            return true;
        }

        public bool AddHiddenObject(string name, object info)
        {
            if(hiddenObjects.ContainsKey(name))
                return false;
            hiddenObjects.Add(name, info);
            return true;
        }

        public T? GetObject<T>(string name)
            where T : class
        {
            return codeObjects.GetValueOrDefault(name) as T;
        }

        public T? GetHiddenObject<T>(string name)
            where T : class
        {
            return hiddenObjects.GetValueOrDefault(name) as T;
        }
    }
}
