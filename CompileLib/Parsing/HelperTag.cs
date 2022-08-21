using CompileLib.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Tag class for representing nested productions
    /// </summary>
    internal class HelperTag
    {
        private static long Counter = 0;

        /// <summary>
        /// An integer to identify the tag
        /// </summary>
        public long ID { get; private set; }
        /// <summary>
        /// The repetition count attribute from which the tag was created
        /// </summary>
        public object ParentAttribute { get; private set; }

        public HelperTag(object parentAttribute)
        {
            ID = Counter++;
            ParentAttribute = parentAttribute;
        }

        public override string? ToString()
        {
            return ProductionBodyElement.ShowHelperTag(this);
        }

        public override bool Equals(object? obj)
        {
            return obj is HelperTag ht && ht.ID == ID;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public Alternation<string, HelperTag> ToTag()
        {
            return new(this);
        }
    }
}
