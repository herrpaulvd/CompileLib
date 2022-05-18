using CompileLib.EmbeddedLanguage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class ConstExpression : Expression
    {
        public string Self { get; private set; }
        public string ConstType { get; private set; }

        private TypeExpression type;
        public override TypeExpression Type => type;

        public ConstExpression(string self, string type, int line, int column) : base(line, column)
        {
            Self = self;
            ConstType = type;
        }
        /*
         * engine = new ParsingEngineBuilder()
        .AddToken("id", @"[[:alpha:]_][[:alnum:]_]*")
        .AddToken("str", @"""(\\.|[^\\""[:cntrl:]])*""")
        .AddToken("char", @"'(\\.|[^\\'[:cntrl:]])'")
        .AddToken("int10", @"[1-9][0-9]*|0")
        .AddToken("int16", @"0x[[:xdigit:]]+")
        .AddToken("int8", @"0[0-7]+")
        .AddToken("int2", @"0b[01]+")
        .AddKeyword("this")
        .AddToken(SpecialTags.TAG_SKIP, "[[:space:]]")
        .AddToken(SpecialTags.TAG_SKIP, @"//[^[:cntrl:]]*")
        .AddProductions<OldSyntax>()
        .Create("program");
         * 
         */

        private static string Rebuild(string s)
        {
            s = s[1..^1];
            StringBuilder sb = new();
            for(int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\')
                {
                    switch (s[++i])
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '0': sb.Append('\0'); break;
                        default: sb.Append(s[i]); break;
                    }
                }
                else
                    sb.Append(s[i]);
            }
            return sb.ToString();
        }

        private static readonly ELType CHAR = ELType.UInt16;
        private static readonly ELType PCHAR = CHAR.MakePointer();

        public override ELExpression CompileRight(CompilationParameters compilation)
        {
            ELExpression parseInt(int fromBase, int skip)
            {
                type = new TypeExpression(-1, -1, "int64", 0);
                long value;
                try
                {
                    value = Convert.ToInt64(Self.Substring(skip), fromBase);
                }
                catch (Exception)
                {
                    throw new CompilationError("Invalid int64 const", Line, Column);
                }
                return compilation.Compiler.MakeConst(value);
            }

            switch(ConstType)
            {
                case "str":
                    type = new TypeExpression(-1, -1, "char", 1);
                    ELDataBuilder buildStr = new();
                    buildStr.AddUnicodeString(Rebuild(Self));
                    buildStr.Add((ushort)0);
                    return compilation.Compiler.AddInitializedData(PCHAR, buildStr);
                case "char":
                    type = new TypeExpression(-1, -1, "char", 0);
                    char c = Rebuild(Self)[0];
                    return compilation.Compiler.MakeConst(c).Cast(CHAR);
                case "int10":
                    return parseInt(10, 0);
                case "int16":
                    return parseInt(16, 2);
                case "int8":
                    return parseInt(8, 1);
                case "int2":
                    return parseInt(2, 2);
                default:
                    throw new NotImplementedException();
            }
        }

        public override ELMemoryCell CompileLeft(CompilationParameters compilation)
        {
            throw new CompilationError("Literal is not a memory cell", Line, Column);
        }
    }
}
