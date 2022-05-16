using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Parsing;
using CompileLib.EmbeddedLanguage;
using CompileLib.Semantics;

namespace TestCompiler
{
    internal class OldSyntax
    {
        private static readonly ELType TChar = ELType.UInt16;
        private static readonly ELType PChar = TChar.MakePointer();
        private static readonly ELType SIZE = ELType.UInt64;
        private static readonly ELStructType TString = new(1, SIZE, PChar);
        private static readonly ELType PString = TString.MakePointer();
        private static readonly ELDataBuilder dataBuilder = new();

        private const int FIELD_LENGTH = 0;
        private const int FIELD_CHARS = 1;

        public class Error : Exception
        {
            public Error(string msg) : base(msg) { }
        }

        public class VariableInfo
        {
            public string Name { get; private set; }
            public ELVariable Self { get; private set; }

            public VariableInfo(string name, ELVariable self)
            {
                Name = name;
                Self = self;
            }
        }

        public class Program
        {
            private Statement[] statements;

            public Program(Statement[] statements)
            {
                this.statements = statements;
            }

            public void Compile(string filename)
            {
                var compiler = new ELCompilerBuilder()
                    .AddMemoryFunctions(out ELFunction malloc, out ELFunction realloc, out ELFunction free, true, true)
                    .AddMemcpy(out ELFunction memcpy)
                    .AddConsoleFunctionsW(out ELFunction ConsoleReadW, out ELFunction ConsoleWriteW)
                    .AddConsoleReadLineW(out ELFunction ConsoleReadLineW)
                    .Create();

                Scope scope = new();

                dataBuilder.Clear();
                var nlstr = "\r\n";
                dataBuilder.AddUnicodeString(nlstr);
                var nl = compiler.AddInitializedData(PChar, dataBuilder);

                // str-concat function
                var strconcat = compiler.CreateFunction(ELType.Void, PString, PString, PString);
                scope.AddHiddenObject("strconcat", strconcat);
                strconcat.Open();
                var a = strconcat.GetParameter(0).PtrToRef();
                var b = strconcat.GetParameter(1).PtrToRef();
                var result = strconcat.GetParameter(2).PtrToRef();
                var alen = a.FieldRef(FIELD_LENGTH);
                var blen = b.FieldRef(FIELD_LENGTH);
                var length = alen + blen;
                result.FieldRef(FIELD_LENGTH).Value = length;
                var s = malloc.Call((length + 1U) * (uint)TChar.Size).Cast(PChar);
                memcpy.Call(s, a.FieldRef(FIELD_CHARS), alen * (uint)TChar.Size);
                memcpy.Call(s + alen, b.FieldRef(FIELD_CHARS), blen * (uint)TChar.Size);
                result.FieldRef(FIELD_CHARS).Value = s;
                compiler.Return();

                // readline function
                var readline = compiler.CreateFunction(ELType.Void, PString);
                scope.AddHiddenObject("readln", readline);
                readline.Open();
                result = readline.GetParameter(0).PtrToRef();
                result.FieldRef(FIELD_CHARS).Value = ConsoleReadLineW.Call(result.FieldRef(FIELD_LENGTH).Address);
                compiler.Return();

                // write function
                var write = compiler.CreateFunction(ELType.PVoid, PString);
                scope.AddHiddenObject("write", write);
                write.Open();
                a = write.GetParameter(0).PtrToRef();
                ConsoleWriteW.Call(a.FieldRef(FIELD_CHARS), a.FieldRef(FIELD_LENGTH));

                // writeln function
                var writeln = compiler.CreateFunction(ELType.PVoid, PString);
                scope.AddHiddenObject("writeln", writeln);
                writeln.Open();
                var suffix = compiler.AddLocalVariable(TString);
                suffix.FieldRef(FIELD_CHARS).Value = nl;
                suffix.FieldRef(FIELD_LENGTH).Value = compiler.MakeConst((uint)nlstr.Length);
                var output = compiler.AddLocalVariable(TString).Address;
                strconcat.Call(writeln.GetParameter(0), suffix.Address, output);
                write.Call(output);

                compiler.OpenEntryPoint();
                foreach (var statement in statements)
                    statement.Compile(scope, compiler);
                compiler.BuildAndSave(filename);
            }
        }

        public abstract class Statement
        {
            public abstract void Compile(Scope scope, ELCompiler compiler);
        }

        public class SimpleStatement : Statement
        {
            private string? variableType;
            private string? variableName;
            private Expression? expression;

            public SimpleStatement(Expression? expression)
            {
                this.expression = expression;
            }

            public SimpleStatement(string? variableType, string? variableName, Expression? expression)
            {
                this.variableType = variableType;
                this.variableName = variableName;
                this.expression = expression;
            }

            public static readonly Statement Empty = new SimpleStatement(null);
            public bool IsEmpty() => expression is null;

            public override void Compile(Scope scope, ELCompiler compiler)
            {
                if (variableType is not null)
                {
                    if (variableType != "string")
                        throw new Error("The only type to be allowed is string");
                    var v = compiler.AddLocalVariable(PString);
                    var vInfo = new VariableInfo(variableName ?? throw new Error("Internal error"), v);
                    if (!scope.AddCodeObject(variableName, vInfo))
                        throw new Error($"A variable with the name {variableName} already exists");
                    if (expression is not null)
                    {
                        var e = expression.Compile(scope, compiler);
                        v.Value = e;
                    }
                }
            }
        }

        public class IOStatement : Statement
        {
            private string operation;
            private Expression expression;

            public IOStatement(string operation, Expression expression)
            {
                this.operation = operation;
                this.expression = expression;
            }

            public override void Compile(Scope scope, ELCompiler compiler)
            {
                if (operation == "write" || operation == "writeln")
                    scope.GetHiddenObject<ELFunction>(operation)?.Call(expression.Compile(scope, compiler));
                else
                {
                    var vExpr = expression as ExprVariable;
                    var v = scope.GetObject<VariableInfo>(vExpr.Name);
                    if (v is null) throw new Error("Undeclared variable");

                    var storage = compiler.AddLocalVariable(TString).Address;
                    scope.GetHiddenObject<ELFunction>(operation).Call(storage);
                    v.Self.Value = storage;
                }
            }
        }

        public abstract class Expression
        {
            public abstract ELExpression Compile(Scope scope, ELCompiler compiler);
        }

        public class ExprVariable : Expression
        {
            public string Name { get; private set; }

            public ExprVariable(string name)
            {
                Name = name;
            }

            public override ELExpression Compile(Scope scope, ELCompiler compiler)
            {
                var v = scope.GetObject<VariableInfo>(Name);
                if (v is null) throw new Error("Undeclared variable");
                return v.Self;
            }
        }

        public class ExprConst : Expression
        {
            public string Value { get; private set; }

            public ExprConst(string value)
            {
                Value = value;
            }

            public override ELExpression Compile(Scope scope, ELCompiler compiler)
            {
                dataBuilder.Clear();
                var s = Value[1..^1];
                var len = s.Length;
                dataBuilder.AddUnicodeString(s);
                var result = compiler.AddLocalVariable(TString);
                result.FieldRef(FIELD_LENGTH).Value = compiler.MakeConst((uint)len);
                result.FieldRef(FIELD_CHARS).Value = compiler.AddInitializedData(PChar, dataBuilder);
                return result.Address;
            }
        }

        public class ExprBinary : Expression
        {
            public string Sign { get; private set; }
            public Expression Left { get; private set; }
            public Expression Right { get; private set; }

            public ExprBinary(string sign, Expression left, Expression right)
            {
                Sign = sign;
                Left = left;
                Right = right;
            }

            public override ELExpression Compile(Scope scope, ELCompiler compiler)
            {
                if (Sign == "=")
                {
                    var vExpr = Left as ExprVariable;
                    var v = scope.GetObject<VariableInfo>(vExpr.Name);
                    if (v is null) throw new Error("Undeclared variable");
                    var e = Right.Compile(scope, compiler);
                    v.Self.Value = e;
                    return e;
                }
                else if (Sign == "+")
                {
                    var v = compiler.AddLocalVariable(TString).Address;
                    scope.GetHiddenObject<ELFunction>("strconcat")
                        .Call(Left.Compile(scope, compiler), Right.Compile(scope, compiler), v);
                    return v;
                }

                throw new Error("Internal error");
            }
        }

        [SetTag("program")]
        public static Program ReadProgram(
            [Many(true)][RequireTags("statement")] Statement[] statements
            )
        {
            return new(statements);
        }

        [SetTag("statement")]
        public static Statement ReadEmptyStatement(
            [Keywords(";")] string semicolon
            )
        {
            return SimpleStatement.Empty;
        }

        [SetTag("statement")]
        public static Statement ReadExpressionStatement(
            [RequireTags("expression")] Expression expression,
            [Keywords(";")] string semicolon
            )
        {
            return new SimpleStatement(expression);
        }

        [SetTag("statement")]
        public static Statement ReadInitializationStatement(
            [RequireTags("id")] string type,
            [RequireTags("id")] string name,
            [Keywords("=")] string sign,
            [RequireTags("expression")] Expression expression,
            [Keywords(";")] string semicolon
            )
        {
            return new SimpleStatement(type, name, expression);
        }

        [SetTag("statement")]
        public static Statement ReadReadStatement(
            [Keywords("readln")] string operation,
            [RequireTags("expression.L")] Expression operand,
            [Keywords(";")] string semicolon
            )
        {
            return new IOStatement(operation, operand);
        }

        [SetTag("statement")]
        public static Statement ReadWriteStatement(
            [Keywords("writeln", "write")] string operation,
            [RequireTags("expression")] Expression operand,
            [Keywords(";")] string semicolon
            )
        {
            return new IOStatement(operation, operand);
        }

        [SetTag("expression.atom")]
        public static Expression ReadAtom(
            [RequireTags("id", "str")] Token token
            )
        {
            if (token.Tag == "id") return new ExprVariable(token.Self);
            return new ExprConst(token.Self);
        }

        [SetTag("expression.pluslike")]
        public static Expression ConvertAtom2PlusLike(
            [RequireTags("expression.atom")] Expression expression
            )
        {
            return expression;
        }

        [SetTag("expression.pluslike")]
        public static Expression ReadPluslike(
            [RequireTags("expression.pluslike")] Expression left,
            [Keywords("+")] string sign,
            [RequireTags("expression.atom")] Expression right
            )
        {
            return new ExprBinary(sign, left, right);
        }

        [SetTag("expression")]
        public static Expression ConvertPluslike2Expression(
            [RequireTags("expression.pluslike")] Expression expression
            )
        {
            return expression;
        }

        [SetTag("expression.L")]
        public static Expression ReadLExpr(
            [RequireTags("id")] string id
            )
        {
            return new ExprVariable(id);
        }

        [SetTag("expression")]
        public static Expression ReadEqExpression(
            [RequireTags("expression.L")] Expression left,
            [Keywords("=")] string sign,
            [RequireTags("expression")] Expression right
            )
        {
            return new ExprBinary(sign, left, right);
        }
    }
}
