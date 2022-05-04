using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public class IncorrectEntryPointException : Exception
    {
        private static string MakeMessage(ELFunction function, bool incorrectReturn, bool incorrectInput)
        {
            string? reason1 = null;
            string? reason2 = null;

            if(incorrectReturn)
                reason1 = $"the function must return either {ELType.Void} or {ELType.UInt32} or {ELType.Int32} but it returns {function.ReturnType}";
            if (incorrectInput)
                reason2 = $"the function must have empty list of arguments but it has {function.ParametersCount} arguments";

            if(reason1 is null)
            {
                if(reason2 is null)
                {
                    reason1 = "of internal error";
                }
                else
                {
                    reason1 = reason2;
                    reason2 = null;
                }
            }

            string message = $"The function is an incorrect entry point because {reason1}";
            if (reason2 is not null) message += $" and {reason2}";
            return message;
        }

        public IncorrectEntryPointException(ELFunction function, bool incorrectReturn, bool incorrectInput)
            : base(MakeMessage(function, incorrectReturn, incorrectInput)) { }
    }
}
