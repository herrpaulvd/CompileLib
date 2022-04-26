using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.ParserTools
{
    internal struct LROpponent
    {
        public bool IsCarry;
        public int? Production; // if null, is the fictive production S' -> S

        public LROpponent(bool isCarry, int? production)
        {
            IsCarry = isCarry;
            Production = production;
        }
    }

    internal class LRConflictException : Exception
    {
        public LROpponent First { get; }
        public LROpponent Second { get; }
        public int[] Way { get; }
        public int? TokenType { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="tokenType">if null, is EOF</param>
        /// <param name="way"></param>
        public LRConflictException(LROpponent first, LROpponent second, int? tokenType, int[] way)
            : base("LR Conflict")
        {
            First = first;
            Second = second;
            Way = way;
            TokenType = tokenType;
        }
    }
}
