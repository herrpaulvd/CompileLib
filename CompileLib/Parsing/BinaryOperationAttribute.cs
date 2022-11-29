using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Used for describing binary infix operation
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class BinaryOperationAttribute : Attribute
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
        /// Flag to define left- or right-associative operation
        /// </summary>
        public bool IsRightAssociative { get; }

        public BinaryOperationAttribute(string sign, int priority, bool isRightAssociative = false)
        {
            Sign = sign;
            Priority = priority;
            IsRightAssociative = isRightAssociative;
        }
    }
}
