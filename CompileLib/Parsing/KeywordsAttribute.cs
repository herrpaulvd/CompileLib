using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Attribute to define a keyword
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class KeywordsAttribute : Attribute
    {
        /// <summary>
        /// The reqired keywords
        /// </summary>
        public ReadOnlyCollection<string> Keywords { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keywords">The required keywords</param>
        public KeywordsAttribute(params string[] keywords)
        {
            Keywords = new(keywords);
        }
    }
}
