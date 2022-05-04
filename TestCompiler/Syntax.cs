using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Parsing;
using CompileLib.EmbeddedLanguage;
using CompileLib.Semantic;

namespace TestCompiler
{
    internal class Syntax
    {
        public static readonly ELType TChar = ELType.UInt16;
        public static readonly ELType TString = TChar.MakePointer();
        public static readonly ELType THandle = ELType.Void.MakePointer();
        public static readonly ELType TDWORD = ELType.UInt32;

        public class Error : Exception
        {
            public Error(string msg) : base(msg) { }
        }

        public class VariableInfo
        {
            public string Name { get; private set; }
            public ELVariable Self { get; private set; }
            public bool Initialized { get; private set; }

            public VariableInfo(string name, ELVariable self)
            {
                Name = name;
                Self = self;
                Initialized = false;
            }

            public void Initialize() { Initialized = true; }
        }

        public class Program
        {
            private Statement[] statements;

            public Program(Statement[] statements)
            {
                this.statements = statements;
            }

            public void Compile(Scope scope, ELCompiler compiler)
            {
                // TODO: console input function
                // maybe malloc required

                var main = compiler.CreateFunction(ELType.Void);
                main.IsEntryPoint = true;
                main.Enter();

                var conin = compiler.AddVariable(THandle);
                scope.AddHiddenObject("conin", conin);
                var conout = compiler.AddVariable(THandle);
                scope.AddHiddenObject("conout", conout);
                var fGetStdHandle = compiler.ImportFunction("kernel32.dll", "GetStdHandle", THandle, TDWORD);
                conin.Value = fGetStdHandle.Call(-10);
                conout.Value = fGetStdHandle.Call(-11);

                foreach (var statement in statements)
                    statement.Compile(scope, compiler);
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
                if(variableType is not null)
                {
                    if (variableType != "string")
                        throw new Error("The only type to be allowed is string");
                    var v = compiler.AddVariable(TString);
                    var vInfo = new VariableInfo(variableName ?? throw new Error("Internal error"), v);
                    if (!scope.AddCodeObject(variableName, vInfo))
                        throw new Error($"A variable with the name {variableName} already exists");
                    if(expression is not null)
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
                // TODO???
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
                throw new NotImplementedException();
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
                throw new NotImplementedException();
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
                throw new NotImplementedException();
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
            [Optional(true)][Keywords("=")] string sign,
            [TogetherWith][RequireTags("expression")] Expression expression,
            [Keywords(";")] string semicolon
            )
        {
            return new SimpleStatement(type, name, expression);
        }

        [SetTag("statement")]
        public static Statement ReadReadStatement(
            [Keywords("readline")] string operation,
            [RequireTags("expression.L")] Expression operand
            )
        {
            return new IOStatement(operation, operand);
        }

        [SetTag("statement")]
        public static Statement ReadWriteStatement(
            [Keywords("writeline", "write")] string operation,
            [RequireTags("expression")] Expression operand
            )
        {
            return new IOStatement(operation, operand);
        }

        [SetTag("expression.atom")]
        public static Expression ReadAtom(
            [RequireTags("id", "string-const")] Token token
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
