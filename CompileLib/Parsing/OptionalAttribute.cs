using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Attribute to define optional child
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class OptionalAttribute : Attribute
    {
        /// <summary>
        /// Read it greedy?
        /// </summary>
        public bool Greedy { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="greedy">Can the sequence of children be empty?</param>
        public OptionalAttribute(bool greedy)
        {
            Greedy = greedy;
        }
    }
}
