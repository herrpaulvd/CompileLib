using CompileLib.Common;
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
    internal class UserHandler : IProductionHandler, IErrorHandler
    {
        private static readonly Type ParsedTypeDef = typeof(Parsed<string>).GetGenericTypeDefinition();

        internal static object? ConvertForPassing(AnyParsed obj, Type dest, MethodInfo method, ParameterInfo parameter)
        {
            if (obj.Self is null)
                return null;
            
            var ot = obj.Self.GetType();
            if (
                dest.IsGenericType
                && dest.GetGenericTypeDefinition() == ParsedTypeDef
                && ot.IsAssignableTo(dest.GenericTypeArguments[0])
                )
                return Helpers.InternalCreateInstance(dest, new object[] { obj });

            if (ot.IsAssignableTo(dest))
                return obj.Self;

            throw new ParsingException(method, parameter, "Cannot assign production result to the parameter");
        }

        private readonly MethodInfo method;
        private readonly bool hasErrorHandler;
        private readonly Func<Parsed<string>, int?> tokenToType;

        public UserHandler(MethodInfo method, bool hasErrorHandler, Func<Parsed<string>, int?> tokenToType)
        {
            this.method = method;
            this.hasErrorHandler = hasErrorHandler;
            this.tokenToType = tokenToType;
        }

        private void AddRange(ParameterInfo[] parameters, ref int ptr, List<object?> args, IEnumerable<AnyParsed> objs)
        {
            foreach (var e in objs)
            {
                var param = parameters[ptr];
                if (e.Self is IGroup g)
                {
                    AddRange(parameters, ref ptr, args, g.Expand());
                    continue;
                }
                else if (e.Self is UnknownArray a)
                {
                    var ptype = param.ParameterType;
                    if (!ptype.IsArray)
                        throw new ParsingException(method, param, "Expected array type");
                    ptype = ptype.GetElementType();
                    Debug.Assert(ptype is not null);
                    if (ptype.IsValueType)
                        throw new ParsingException(method, param, "Expected no value type of array elements");
                    args.Add(a.Cast(ptype, parsed => ConvertForPassing(parsed, ptype, method, param)));
                }
                else
                {
                    args.Add(ConvertForPassing(e, param.ParameterType, method, param));
                }
                ptr++;
            }
        }

        public object? Handle(AnyParsed[] children, ref string tag)
        {
            var parameters = method.GetParameters();
            List<object?> args = new();
            int ptr = 0;
            AddRange(parameters, ref ptr, args, children);
            if (hasErrorHandler)
                args.Add(null);
            return method.Invoke(null, args.ToArray());
        }

        public ErrorHandlingDecision Handle(AnyParsed[] prefix, Parsed<string> nextToken)
        {
            var parameters = method.GetParameters();
            List<object?> args = new();
            int ptr = 0;
            AddRange(parameters, ref ptr, args, prefix);
            for (int i = ptr; i < parameters.Length - 1; i++)
                args.Add(null);

            var userDecider = new ErrorHandlingDecider(nextToken, tokenToType);
            args.Add(userDecider);
            method.Invoke(null, args.ToArray());
            return userDecider.Result;
        }
    }
}
