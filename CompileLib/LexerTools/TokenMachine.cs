using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.LexerTools
{
    internal class TokenMachine : IMachine
    {
        private readonly string source;
        private int index;

        public TokenMachine(string source)
        {
            this.source = source;
        }

        public bool StateIsFinal => index == source.Length;

        public void Start()
        {
            index = 0;
        }

        public bool Tact(char c)
        {
            if (index == source.Length || index < 0)
            {
                index = -1;
                return false;
            }

            if(source[index] == c)
            {
                index++;
                return true;
            }

            index = -1;
            return false;
        }
    }
}
