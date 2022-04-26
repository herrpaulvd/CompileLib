using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Used for tagging parameters to require some tag
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class RequireTagsAttribute : Attribute
    {
        /// <summary>
        /// Required tags
        /// </summary>
        public ReadOnlyCollection<string> Tags { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the tag</param>
        public RequireTagsAttribute(params string[] tags)
        {
            Tags = new ReadOnlyCollection<string>(tags);
        }
    }
}
