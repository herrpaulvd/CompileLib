using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Many children
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ManyAttribute : Attribute
    {
        /// <summary>
        /// Can the sequence of children be empty?
        /// </summary>
        public bool CanBeEmpty { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canBeEmpty">Can the sequence of children be empty?</param>
        public ManyAttribute(bool canBeEmpty)
        {
            CanBeEmpty = canBeEmpty;
        }
    }
}
