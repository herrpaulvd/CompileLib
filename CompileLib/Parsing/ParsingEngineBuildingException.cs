using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Exception throwed by ParsingEngineBuilder
    /// </summary>
    public class ParsingEngineBuildingException : Exception
    {
        public ParsingEngineBuildingException(string message) : base(message)
        {
        }

        public ParsingEngineBuildingException(MethodInfo method, ParameterInfo parameter, string message)
            : this($"[Method {method.DeclaringType.Name}::{method.Name}, parameter {parameter.Name}] {message}")
        {
        }

        public ParsingEngineBuildingException(MethodInfo method, string message)
            : this($"[Method {method.DeclaringType.Name}::{method.Name}] {message}")
        {
        }
    }
}
