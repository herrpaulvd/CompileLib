using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Priority of production. Any integer value is allowed. Without the attribute, it is set to 0
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ProductionPriorityAttribute : Attribute
    {
        /// <summary>
        /// Priority of the production
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority">Priority of the production</param>
        public ProductionPriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }
}
