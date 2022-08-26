using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompileLib.ParserTools;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Parsing result class
    /// </summary>
    /// <typeparam name="T">Type of token or non-token parsing result</typeparam>
    public sealed class Parsed<T> where T : class
    {
        /// <summary>
        /// Parsed<T> is actually a shell class for AnyParsed struct
        /// </summary>
        private AnyParsed itself;

        public string Tag => itself.Tag;
        public T? Self => (T?)itself.Self;
        public int Line => itself.Line;
        public int Column => itself.Column;

        public Parsed(string tag, T? self, int line, int column)
        {
            itself = new(tag, self, line, column);
        }

        internal Parsed(AnyParsed itself)
        {
            this.itself = itself;
        }
    }
}
