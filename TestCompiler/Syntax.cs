using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Parsing;
using CompileLib.Semantics;

using TestCompiler.CodeObjects;

namespace TestCompiler
{
    internal class Syntax
    {
        [SetTag("global")]
        public static CodeObject ReadGlobal(
            [Many(true)][RequireTags("class")] CodeObject[] components
            )
        {
            CodeObject global = new("global", "global", 1, 1);
            foreach (var component in components)
            {
                component.AddRelation("parent", global);
                global.AddRelation("child", component);
            }

            return global;
        }

        private static MemberVisibility Str2Visibility(string? s)
            => s switch
            {
                null => MemberVisibility.Private,
                "private" => MemberVisibility.Private,
                "protected" => MemberVisibility.Protected,
                "public" => MemberVisibility.Public,
                _ => throw new NotImplementedException()
            };

        [SetTag("class")]
        public static CodeObject ReadClass(
            [Optional(false)][RequireTags("visibility-mod")] Token? visMod,
            [Keywords("class")] Token kw,
            [RequireTags("id")] string id,
            [Optional(false)][Keywords("(")] string brParamOpen,
            [TogetherWith][RequireTags("call-args")] List<Expression> parameters,
            [TogetherWith][Keywords(")")] string brParamClose,
            [Optional(false)][Keywords(":")] string inherited,
            [TogetherWith][RequireTags("expr")] Expression baseClass,
            [Keywords("{")] string brOpen,
            [Many(true)][RequireTags("class", "method", "field", "constructor")] CodeObject[] components,
            [Keywords("}")] string brClose
            )
        {
            var firstToken = visMod ?? kw;
            Expression[] paramArray;
            if(parameters is null) paramArray = Array.Empty<Expression>();
            else paramArray = parameters.ToArray();

            return new Class(id, firstToken.Line, firstToken.Column, baseClass, Str2Visibility(visMod?.Self), paramArray, components);
        }

        [SetTag("visibility-mod")]
        public static Token ReadVisibilityModifier(
            [Keywords("public", "private", "protected")] Token self
            )
        {
            return self;
        }

        [SetTag("field")]
        public static CodeObject ReadField(
            [Optional(false)][RequireTags("visibility-mod")] Token? visMod,
            [Optional(false)][Keywords("static")] Token? kwStatic,
            [RequireTags("expr")] Expression type,
            [RequireTags("id")] string id,
            [Optional(false)][Keywords("=")] string eq,
            [TogetherWith][RequireTags("expr")] Expression expr,
            [Keywords(";")] string close
            )
        {
            var firstToken = visMod ?? kwStatic;
            return new Field(id, firstToken?.Line ?? type.Line, firstToken?.Column ?? type.Column, Str2Visibility(visMod?.Self), kwStatic is not null, type, expr);
        }

        [SetTag("method")]
        public static CodeObject ReadMethod(
            [Optional(false)][RequireTags("visibility-mod")] Token? visMod,
            [Optional(false)][Keywords("static")] Token? kwStatic,
            [RequireTags("expr")] Expression type,
            [RequireTags("id")] string id,
            [Keywords("(")] string brOpen,
            [Optional(false)][RequireTags("method.params")] List<Parameter> parameters,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] Statement statement
            )
        {
            var firstToken = visMod ?? kwStatic;
            parameters.Reverse();
            return new Method(id, firstToken?.Line ?? type.Line, firstToken?.Column ?? type.Column, Str2Visibility(visMod?.Self), kwStatic is not null, type, statement, parameters.ToArray());
        }

        [SetTag("method.params")]
        public static List<Parameter> ReadMethodParameters(
            [RequireTags("expr")] Expression type,
            [RequireTags("id")] string id,
            [Optional(false)][Keywords(",")] string separator,
            [TogetherWith][RequireTags("method.params")] List<Parameter> tail
            )
        {
            Parameter parameter = new(id, type.Line, type.Column, type);
            tail.Add(parameter);
            return tail;
        }

        [SetTag("constructor")]
        public static CodeObject ReadConstructor(
            [Optional(false)][RequireTags("visibility-mod")] Token? visMod,
            [Optional(false)][Keywords("static")] Token? kwStatic,
            [RequireTags("expr")] Expression type,
            [Keywords("(")] string brOpen,
            [Optional(false)][RequireTags("method.params")] List<Parameter> parameters,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] Statement statement
            )
        {
            var firstToken = visMod ?? kwStatic;
            parameters.Reverse();
            return new Method("", firstToken?.Line ?? type.Line, firstToken?.Column ?? type.Column, Str2Visibility(visMod?.Self), kwStatic is not null, type, statement, parameters.ToArray());
        }

        [SetTag("statement")]
        public static Statement ReadSimpleStatement(
            [Optional(false)][RequireTags("expr")] Expression type,
            [TogetherWith][RequireTags("id")] string id,
            [TogetherWith][Keywords("=")] string eq,
            [Optional(false)][RequireTags("expr")] Expression expr,
            [Keywords(";")] Token close
            )
        {
            LocalVariable? localVariable = null;
            if (type is not null)
            {
                localVariable = new(id, type.Line, type.Column, type);
            }
            int line = type?.Line ?? expr?.Line ?? close.Line;
            int column = type?.Column ?? expr?.Column ?? close.Column;
            return new SimpleStatement(line, column, localVariable, expr);
        }

        [SetTag("statement")]
        public static Statement ReadStatementBlock(
            [Keywords("{")] Token brOpen,
            [Many(true)][RequireTags("statement")] Statement[] statements,
            [Keywords("}")] string brClose
            )
        {
            return new BlockStatement(brOpen.Line, brOpen.Column, statements);
        }

        [SetTag("statement")]
        public static Statement ReadIfStatement(
            [Keywords("if")] Token kw,
            [Keywords("(")] string brOpen,
            [RequireTags("expr")] Expression condition,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] Statement ifBranch,
            [Optional(true)][Keywords("else")] string kwElse,
            [TogetherWith][RequireTags("statement")] Statement elseBranch
            )
        {
            return new IfStatement(kw.Line, kw.Column, condition, ifBranch, elseBranch);
        }

        [SetTag("statement")]
        public static Statement ReadWhileStatement(
            [Keywords("while")] Token kw,
            [Keywords("(")] string brOpen,
            [RequireTags("expr")] Expression condition,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] Statement body
            )
        {
            return new WhileStatement(kw.Line, kw.Column, condition, body, false);
        }

        [SetTag("statement")]
        public static Statement ReadDoWhileStatement(
            [Keywords("do")] Token kw,
            [RequireTags("statement")] Statement body,
            [Keywords("while")] string kwWhile,
            [Keywords("(")] string brOpen,
            [RequireTags("expr")] Expression condition,
            [Keywords(")")] string brClose,
            [Keywords(";")] string close
            )
        {
            return new WhileStatement(kw.Line, kw.Column, condition, body, true);
        }

        [SetTag("statement")]
        public static Statement ReadForStatement(
            [Keywords("for")] Token kw,
            [Keywords("(")] string brOpen,
            [RequireTags("expr")] Expression init,
            [Keywords(";")] string sep1,
            [RequireTags("expr")] Expression cond,
            [Keywords(";")] string sep2,
            [RequireTags("expr")] Expression step,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] Statement body
            )
        {
            return new ForStatement(kw.Line, kw.Column, init, cond, step, body);
        }

        [SetTag("call-args")]
        public static List<Expression> ReadCallArgs(
            [RequireTags("expr")] Expression expr,
            [Optional(false)][Keywords(",")] string separator,
            [TogetherWith][RequireTags("call-args")] List<Expression> tail
            )
        {
            if (tail is null) return new List<Expression> { expr };
            tail.Add(expr);
            return tail;
        }

        [SetTag("expr-atom")]
        public static Expression ReadExprInBrackets(
            [Keywords("(")] Token brOpen,
            [RequireTags("expr")] Expression expr,
            [Keywords(")")] string brClose
            )
        {
            expr.ChangePosition(brOpen.Line, brOpen.Column);
            return expr;
        }

        [SetTag("expr-atom")]
        public static Expression ReadID(
            [RequireTags("id")] Token id
            )
        {
            return new IdExpression(id.Self, id.Line, id.Column);
        }

        [SetTag("expr-atom")]
        public static Expression ReadConst(
            [RequireTags("int10", "int16", "int8", "int2", "str", "char")] Token token
            )
        {
            return new ConstExpression(token.Self, token.Tag, token.Line, token.Column);
        }

        [SetTag("expr-call")]
        public static Expression ConvertAtom2Call(
            [RequireTags("expr-atom")] Expression expr
            )
        {
            return expr;
        }

        [SetTag("expr-call")]
        public static Expression ReadRoundCall(
            [RequireTags("expr-call")] Expression callee,
            [Keywords("(")] string brOpen,
            [Optional(false)][RequireTags("call-args")] List<Expression> args,
            [RequireTags(")")] string brClose
            )
        {
            Expression[] argsArray;
            if(args is null) argsArray = Array.Empty<Expression>();
            else argsArray = args.ToArray();
            return new CallExpression(callee, argsArray, brOpen, callee.Line, callee.Column);
        }

        [SetTag("expr-call")]
        public static Expression ReadSquareCall(
            [RequireTags("expr-call")] Expression callee,
            [Keywords("[")] string brOpen,
            [Optional(false)][RequireTags("call-args")] List<Expression> args,
            [RequireTags("]")] string brClose
            )
        {
            Expression[] argsArray;
            if (args is null) argsArray = Array.Empty<Expression>();
            else argsArray = args.ToArray();
            return new CallExpression(callee, argsArray, brOpen, callee.Line, callee.Column);
        }

        [SetTag("expr-call")]
        public static Expression ReadDotExpression(
            [RequireTags("expr-call")] Expression left,
            [Keywords(".")] string operation,
            [RequireTags("id")] string id
            )
        {
            return new DotExpression(left, id, operation, left.Line, left.Column);
        }

        [SetTag("expr-unary")]
        public static Expression ConvertCall2Unary(
            [RequireTags("expr-call")] Expression expr
            )
        {
            return expr;
        }

        [SetTag("expr-unary")]
        public static Expression ReadUnaryExpression(
            [Keywords("!", "~", "-", "+", "*", "&")] Token operation,
            [RequireTags("expr-unary")] Expression operand
            )
        {
            return new UnaryExpression(operand, operation.Self, operation.Line, operation.Column);
        }

        [SetTag("expr-mul")]
        public static Expression ConvertUnary2Mul(
            [RequireTags("expr-unary")] Expression expr
            )
        {
            return expr;
        }

        [SetTag("expr-mul")]
        public static Expression ReadMulExpression(
            [RequireTags("expr-mul")] Expression left,
            [Keywords("*", "/")] Token operation,
            [RequireTags("expr-unary")] Expression right
            )
        {
            return new BinaryExpression(left, right, operation.Self, left.Line, left.Column);
        }

        [SetTag("expr-add")]
        public static Expression ConvertMul2Add(
            [RequireTags("expr-mul")] Expression expr
            )
        {
            return expr;
        }

        [SetTag("expr-add")]
        public static Expression ReadAddExpression(
            [RequireTags("expr-add")] Expression left,
            [Keywords("+", "-")] Token operation,
            [RequireTags("expr-mul")] Expression right
            )
        {
            return new BinaryExpression(left, right, operation.Self, left.Line, left.Column);
        }

        [SetTag("expr-cmp")]
        public static Expression ConvertAdd2Cmp(
            [RequireTags("expr-add")] Expression expr
            )
        {
            return expr;
        }

        [SetTag("expr-cmp")]
        public static Expression ReadCmpExpression(
            [RequireTags("expr-cmp")] Expression left,
            [Keywords("==", "!=", "<", ">", "<=", ">=")] Token operation,
            [RequireTags("expr-add")] Expression right
            )
        {
            return new BinaryExpression(left, right, operation.Self, left.Line, left.Column);
        }

        [SetTag("expr-log")]
        public static Expression ConvertCmp2Log(
            [RequireTags("expr-cmp")] Expression expr
            )
        {
            return expr;
        }

        [SetTag("expr-log")]
        public static Expression ReadLogExpression(
            [RequireTags("expr-log")] Expression left,
            [Keywords("&", "&&", "|", "||")] Token operation,
            [RequireTags("expr-cmp")] Expression right
            )
        {
            return new BinaryExpression(left, right, operation.Self, left.Line, left.Column);
        }

        [SetTag("expr")]
        public static Expression ConvertLog2Assign(
            [RequireTags("expr-log")] Expression expr
            )
        {
            return expr;
        }

        [SetTag("expr")]
        public static Expression ReadAssignExpression(
            [RequireTags("expr-log")] Expression left,
            [Keywords("=")] Token operation,
            [RequireTags("expr")] Expression right
            )
        {
            return new BinaryExpression(left, right, operation.Self, left.Line, left.Column);
        }
    }
}
