using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Used for tagging methods to include them in a group
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SetTagAttribute : Attribute
    {
        /// <summary>
        /// Name of the tag
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the tag</param>
        public SetTagAttribute(string name)
        {
            Name = name;
        }
    }
}
