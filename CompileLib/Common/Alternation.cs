using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Common
{
    /// <summary>
    /// Simple struct to represent A or B.
    /// NB! A is not subclass of B and vice versa.
    /// NB! The requirement is not checked.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    internal struct Alternation<TFirst, TSecond> where TFirst : class where TSecond : class
    {
        private object _value;

        public Alternation(TFirst value) => _value = value;
        public Alternation(TSecond value) => _value = value;

        public bool FirstType() => _value is TFirst;
        public bool SecondType() => _value is TSecond;

        public TFirst First => (TFirst)_value;
        public TSecond Second => (TSecond)_value;

        public override string? ToString()
        {
            return _value.ToString();
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Alternation<TFirst, TSecond> other && _value.Equals(other._value);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
