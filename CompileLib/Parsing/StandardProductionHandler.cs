using CompileLib.ParserTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Internal handler for user's production methods
    /// </summary>
    internal class StandardProductionHandler : IProductionHandler, IErrorHandler
    {
        private readonly MethodInfo method;
        private readonly bool hasErrorHandler;
        private readonly Func<int, string> typeToStr;
        private readonly Func<Token, int> strToType;

        public StandardProductionHandler(MethodInfo method, bool hasErrorHandler, Func<int, string> typeToStr, Func<Token, int> strToType)
        {
            this.method = method;
            this.hasErrorHandler = hasErrorHandler;
            this.typeToStr = typeToStr;
            this.strToType = strToType;
        }

        private void AddRange(ParameterInfo[] parameters, ref int ptr, List<object?> args, IEnumerable<object?> objs)
        {
            Debug.Assert(method.DeclaringType is not null);
            foreach (var c in objs)
            {
                if (c is null)
                    args.Add(null);
                else if (c is Common.Token t)
                {
                    if (typeof(Token).IsAssignableTo(parameters[ptr].ParameterType))
                        args.Add(new Token(typeToStr(t.Type), t.Self, t.Line, t.Column));
                    else if (typeof(string).IsAssignableTo(parameters[ptr].ParameterType))
                        args.Add(t.Self);
                    else
                        throw new ParsingException(method, parameters[ptr], "Cannot represent token via the parameter type");
                }
                else if (c is IGroup g)
                {
                    AddRange(parameters, ref ptr, args, g.Expand());
                    continue;
                }
                else if (c is UnknownArray a)
                {
                    var ptype = parameters[ptr].ParameterType;
                    if (!ptype.IsArray)
                        throw new ParsingException(method, parameters[ptr], "Expected array type");
                    ptype = ptype.GetElementType();
                    Debug.Assert(ptype is not null);
                    if (ptype.IsValueType)
                        throw new ParsingException(method, parameters[ptr], "Expected no value type of array elements");
                    if (!a.Check(ptype))
                        throw new ParsingException(method, parameters[ptr], "Cannot represent one or more elements via the array element type");
                    args.Add(a.Cast(ptype, typeToStr));
                }
                else
                {
                    if (c.GetType().IsAssignableTo(parameters[ptr].ParameterType))
                        args.Add(c);
                    else
                        throw new ParsingException(method, parameters[ptr], "Cannot represent non-token via the parameter type");
                }
                ptr++;
            }
        }

        public object? Handle(object?[] children)
        {
            var parameters = method.GetParameters();
            List<object?> args = new();
            int ptr = 0;
            AddRange(parameters, ref ptr, args, children);
            if (hasErrorHandler)
                args.Add(null);
            return method.Invoke(null, args.ToArray());
        }

        public void Handle(object?[] prefix, ParserTools.ErrorHandlingDecider decider)
        {
            var parameters = method.GetParameters();
            List<object?> args = new();
            int ptr = 0;
            AddRange(parameters, ref ptr, args, prefix);
            for (int i = ptr; i < parameters.Length - 1; i++)
                args.Add(null);

            var userDecider = new ErrorHandlingDecider(decider, typeToStr(decider.NextToken.Type), strToType);
            args.Add(userDecider);
            method.Invoke(null, args.ToArray());
        }
    }
}
