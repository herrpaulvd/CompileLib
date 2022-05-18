using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;
using CompileLib.EmbeddedLanguage;

namespace TestCompiler.CodeObjects
{
    internal class ThisClosure
    {
        public ELExpression? This { get; private set; }
        public Method Method { get; private set; }

        public ThisClosure(ELExpression? @this, Method method)
        {
            This = @this;
            Method = method;
        }
    }
}
