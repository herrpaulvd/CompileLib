using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Exception throwed while parsing
    /// </summary>
    public class ParsingException : Exception
    {
        public ParsingException(string msg) : base(msg)
        {
        }

        public ParsingException(MethodInfo method, ParameterInfo parameter, string message)
            : this($"[Method {method.DeclaringType.Name}::{method.Name}, parameter {parameter.Name}] {message}")
        {
        }

        public ParsingException(MethodInfo method, string message)
            : this($"[Method {method.DeclaringType.Name}::{method.Name}] {message}")
        {
        }
    }
}
