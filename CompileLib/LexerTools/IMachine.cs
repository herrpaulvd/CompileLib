using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.LexerTools
{
    internal interface IMachine
    {
        void Start();
        bool Tact(char c);
        bool StateIsFinal { get; }
    }
}
