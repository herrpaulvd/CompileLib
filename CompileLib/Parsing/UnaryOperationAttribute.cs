using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Used for describing unary prefix operation
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class UnaryOperationAttribute : Attribute
    {
        /// <summary>
        /// Sign of the operation
        /// </summary>
        public string Sign { get; }
        /// <summary>
        /// Priority of the operation
        /// </summary>
        public int Priority { get; }
        /// <summary>
        /// Flag to define prefix or suffix unary operation
        /// </summary>
        public bool IsSuffix { get; }

        public UnaryOperationAttribute(string sign, int priority, bool isSuffix = false)
        {
            Sign = sign;
            Priority = priority;
            IsSuffix = isSuffix;
        }
    }
}
