using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Parsing;

namespace TestCompiler
{
    internal class Syntax
    {
        public class Program
        {
            private Statement[] statements;

            public Program(Statement[] statements)
            {
                this.statements = statements;
            }
        }

        public abstract class Statement
        {

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
        }

        public abstract class Expression
        {

        }

        public class ExprVariable : Expression
        {
            public string Name { get; private set; }

            public ExprVariable(string name)
            {
                Name = name;
            }
        }

        public class ExprConst : Expression
        {
            public string Value { get; private set; }

            public ExprConst(string value)
            {
                Value = value;
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
