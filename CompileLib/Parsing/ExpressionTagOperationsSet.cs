using CompileLib.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    internal class ExpressionTagOperationsSet
    {
        public const int BinaryMethodParamsCount = 3;
        public const int UnaryMethodParamsCount = 2;

        [Flags]
        private enum OperationType
        {
            Binary = 1,
            Right = 2,

            Empty = 0,
            
            UnaryPrefix = Empty,
            UnarySuffix = Right,
            BinaryLeft = Binary,
            BinaryRight = Binary | Right
        };

        private struct Operation
        {
            public string Sign;
            public int Priority;
            public OperationType Type;
            public MethodInfo Handler;

            public Operation(string sign, int priority, bool binary, bool right, MethodInfo handler)
            {
                Sign = sign;
                Priority = priority;
                Type = (binary ? OperationType.Binary : OperationType.Empty) | (right ? OperationType.Right : OperationType.Empty);
                Handler = handler;
            }

            public bool Binary => (Type & OperationType.Binary) > 0;
            public bool Right => (Type & OperationType.Right) > 0;
        }

        public string Tag { get; }
        private readonly SortedDictionary<string, Operation> signToBinaryOperation = new();
        private readonly SortedDictionary<string, Operation> signToPrefixOperation = new();
        private readonly SortedDictionary<string, Operation> signToSuffixOperation = new();
        private readonly SortedSet<int>[] typeToPriorities =
        {
            new(), new(), new(), new()
        };

        public ExpressionTagOperationsSet(string tag)
        {
            Tag = tag;
        }

        private Exception AlreadyExistsError(string sign, string toadd, string added)
        {
            return new ParsingEngineBuildingException($"Cannot add {toadd} operation '{sign}' applicable to expression {Tag}: a {added} operation with the sign already exists");
        }

        public void AddOperation(MethodInfo handler, string sign, int priority, bool binary, bool right)
        {
            Operation result = new(sign, priority, binary, right, handler);
            for (int i = 0; i < 4; i++)
                if (i != (int)result.Type && typeToPriorities[i].Contains(priority))
                    throw new ParsingEngineBuildingException(handler, $"Operations can have the same priority if and only if they have equal number of operands and equal associativity/equal position of operand");

            if(binary)
            {
                if (signToBinaryOperation.ContainsKey(sign))
                    throw AlreadyExistsError(sign, "binary", "binary");
                if (signToSuffixOperation.ContainsKey(sign))
                    throw AlreadyExistsError(sign, "binary", "suffix unary");
                signToBinaryOperation.Add(sign, result);
            }
            else if(right)
            {
                if (signToBinaryOperation.ContainsKey(sign))
                    throw AlreadyExistsError(sign, "suffix unary", "binary");
                if (signToSuffixOperation.ContainsKey(sign))
                    throw AlreadyExistsError(sign, "suffix unary", "suffix unary");
                signToSuffixOperation.Add(sign, result);
            }
            else
            {
                if (signToPrefixOperation.ContainsKey(sign))
                    throw AlreadyExistsError(sign, "prefix unary", "prefix unary");
                signToPrefixOperation.Add(sign, result);
            }
            typeToPriorities[(int)result.Type].Add(priority);
        }

        public void GetProductions(List<Production> productionsArray, List<(int, string)> foldingBans, List<(int, string)> carryBans)
        {
            var binaries = signToBinaryOperation.Values.ToArray();
            var prefs = signToPrefixOperation.Values.ToArray();
            var suffs = signToSuffixOperation.Values.ToArray();

            foreach(var op in binaries)
            {
                int index = productionsArray.Count;
                var parameters = op.Handler.GetParameters();
                ProductionBodyElement operation = new()
                {
                    TagType = new KeywordsAttribute(op.Sign),
                    RepetitionCount = SingleAttribute.Instance,
                    Method = op.Handler,
                    Parameter = parameters[1]
                };
                ProductionBodyElement left = new()
                {
                    TagType = new RequireTagsAttribute(Tag),
                    RepetitionCount = SingleAttribute.Instance,
                    Method = op.Handler,
                    Parameter = parameters[0]
                };
                var right = left;
                right.Parameter = parameters[2];
                Production result = new()
                {
                    Tag = Tag.ToTag<HelperTag>(),
                    HasErrorHandler = parameters.Length != BinaryMethodParamsCount,
                    Greedy = 0,
                    Divisor = 0,
                    Body = new() { left, operation, right },
                    Handler = op.Handler
                };
                productionsArray.Add(result);

                foreach(var op2 in binaries.Concat(suffs))
                {
                    var tuple = (index, op2.Sign);
                    if (op.Priority > op2.Priority)
                        carryBans.Add(tuple);
                    else if (op.Priority < op2.Priority)
                        foldingBans.Add(tuple);
                    else if (op.Right)
                        foldingBans.Add(tuple);
                    else
                        carryBans.Add(tuple);
                }
            }

            foreach(var op in prefs)
            {
                int index = productionsArray.Count;
                var parameters = op.Handler.GetParameters();
                ProductionBodyElement operation = new()
                {
                    TagType = new KeywordsAttribute(op.Sign),
                    RepetitionCount = SingleAttribute.Instance,
                    Method = op.Handler,
                    Parameter = parameters[0]
                };
                ProductionBodyElement operand = new()
                {
                    TagType = new RequireTagsAttribute(Tag),
                    RepetitionCount = SingleAttribute.Instance,
                    Method = op.Handler,
                    Parameter = parameters[1]
                };
                Production result = new()
                {
                    Tag = Tag.ToTag<HelperTag>(),
                    HasErrorHandler = parameters.Length != UnaryMethodParamsCount,
                    Greedy = 0,
                    Divisor = 0,
                    Body = new() { operation, operand },
                    Handler = op.Handler
                };
                productionsArray.Add(result);

                foreach (var op2 in binaries.Concat(suffs))
                {
                    var tuple = (index, op2.Sign);
                    if (op.Priority > op2.Priority)
                        carryBans.Add(tuple);
                    else
                        foldingBans.Add(tuple);
                }
            }

            foreach (var op in suffs)
            {
                int index = productionsArray.Count;
                var parameters = op.Handler.GetParameters();
                ProductionBodyElement operation = new()
                {
                    TagType = new KeywordsAttribute(op.Sign),
                    RepetitionCount = SingleAttribute.Instance,
                    Method = op.Handler,
                    Parameter = parameters[1]
                };
                ProductionBodyElement operand = new()
                {
                    TagType = new RequireTagsAttribute(Tag),
                    RepetitionCount = SingleAttribute.Instance,
                    Method = op.Handler,
                    Parameter = parameters[0]
                };
                Production result = new()
                {
                    Tag = Tag.ToTag<HelperTag>(),
                    HasErrorHandler = parameters.Length != UnaryMethodParamsCount,
                    Greedy = 0,
                    Divisor = 0,
                    Body = new() { operation, operand },
                    Handler = op.Handler
                };
                productionsArray.Add(result);
            }
        }
    }
}
