using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Priority of error handling by this method. Any integer value is allowed. Without the attribute, it is set to 0
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ErrorHandlingPriorityAttribute : Attribute
    {
        /// <summary>
        /// Priority of error handling by the method
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Priority of error handling by the method
        /// </summary>
        /// <param name="priority"></param>
        public ErrorHandlingPriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }
}
