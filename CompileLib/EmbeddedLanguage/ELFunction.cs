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
        private int context;
        private ELCompiler compiler;

        public int ParametersCount => parameters.Length;
        public ELVariable GetParameter(int index) => parameters[index];
        public ELType ReturnType => returnType;
        public string? Dll => dll;
        public string? Name => name;
        internal ELCompiler Compiler => compiler;

        internal ELFunction(ELCompiler compiler, int context, ELType returnType, ELVariable[] parameters)
        {
            this.compiler = compiler;
            this.returnType = returnType;
            this.context = context;
            this.parameters = parameters;
        }

        internal ELFunction(ELCompiler compiler, int context, string dll, string name, ELType returnType, ELVariable[] parameters)
            : this(compiler, context, returnType, parameters)
        {
            this.dll = dll;
            this.name = name;
        }

        public void Open()
        {
            if (dll is null)
                compiler.Open(context);
            else
                throw new ArgumentException("Cannot open imported function", "function");
        }

        public ELExpression Call(params ELExpression[] args)
        {
            if (args.Length != parameters.Length)
                throw new ArgumentException($"Invalid args count: found: {args.Length}, required: {parameters.Length}", nameof(args));
            for (int i = 0; i < args.Length; i++)
                if (!args[i].Type.IsAssignableTo(parameters[i].Type))
                    throw new ArgumentException($"Argument #{i}: cannot assign {args[i].Type} to {parameters[i].Type}");

            return compiler.AddExpression(new ELFunctionCall(this, args.Select(a => compiler.TestContext(a, "arg") ?? a).ToArray()));
        }
    }
}
