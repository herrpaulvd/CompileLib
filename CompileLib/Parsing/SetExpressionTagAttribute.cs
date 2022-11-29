using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Used for tagging methods to identify operations' production.
    /// Left part and operands are the same being described with the attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SetExpressionTagAttribute : Attribute
    {
        /// <summary>
        /// Name of the tag
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the tag</param>
        public SetExpressionTagAttribute(string name)
        {
            Name = name;
        }
    }
}
