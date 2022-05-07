using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public class ELFunction
    {
        private string? dll;
        private string? name;
        private ELVariable[] parameters;
        private ELType returnType;
        private ELCodeContext context = new();
        private ELCompiler compiler;

        public int ParametersCount => parameters.Length;
        public ELVariable GetParameter(int index) => parameters[index];
        public ELType ReturnType => returnType;
        public string? Dll => dll;
        public string? Name => name;
        internal ELCompiler Compiler => compiler;

        internal ELFunction(ELCompiler compiler, ELType returnType, ELType[] parameterTypes)
        {
            this.compiler = compiler;
            this.returnType = returnType;
            parameters = parameterTypes.Select(t => new ELVariable(compiler, t)).ToArray();
        }

        internal ELFunction(ELCompiler compiler, string dll, string name, ELType returnType, ELType[] parameterTypes)
            : this(compiler, returnType, parameterTypes)
        {
            this.dll = dll;
            this.name = name;
        }

        public bool IsEntryPoint
        {
            get => ReferenceEquals(this, compiler.EntryPoint);
            set
            {
                if (value)
                {
                    bool incorrectReturn =
                        returnType != ELType.Void
                        && returnType != ELType.Int32
                        && returnType != ELType.UInt32;
                    bool incorrectInput = parameters.Length != 0;

                    if (incorrectInput || incorrectReturn)
                        throw new IncorrectEntryPointException(this, incorrectReturn, incorrectInput);

                    compiler.EntryPoint = this;
                }
                else if(IsEntryPoint)
                    compiler.EntryPoint = null;
            }
        }

        public void Enter() => compiler.CurrentContext = context;

        public ELExpression Call(params ELExpression[] args)
        {
            if (args.Length != parameters.Length)
                throw new ArgumentException($"Invalid args count: found: {args.Length}, required: {parameters.Length}", nameof(args));
            for (int i = 0; i < args.Length; i++)
                if (args[i].Type != parameters[i].Type)
                    throw new ArgumentException($"Argument #{i}: found type: {args[i].Type}, required: {parameters[i].Type}");

            var result = new ELFunctionCall(this, args);
            compiler.CurrentContext?.AddExpression(result);
            return result;
        }
    }
}
