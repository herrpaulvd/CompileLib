using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public struct ELLabel
    {
        internal int Context;
        internal int ID;

        public ELLabel(int context, int iD)
        {
            Context = context;
            ID = iD;
        }
    }
}
