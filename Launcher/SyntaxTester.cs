using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Parsing;

namespace Launcher
{
    internal class SyntaxTester
    {
        private static string Comma(IEnumerable<string> objs) => string.Join(',', objs ?? Array.Empty<string>());
        private static string Space(IEnumerable<string> objs) => string.Join(' ', objs ?? Array.Empty<string>());

        [SetTag("program")]
        public static string Program(
            [Many(true)][RequireTags("class", "method")] string[] members
            )
        {
            Console.WriteLine($"Read whole program, global members: {Comma(members)}");
            return "program";
        }

        [SetTag("mods")]
        public static string[] Mods(
            [Many(true)][Keywords("public", "internal", "protected", "private", "static")] string[] modifiers
            )
        {
            return modifiers;
        }

        [SetTag("class")]
        public static string Class(
            [RequireTags("mods")] string[] modifiers,
            [Keywords("class")] string keyword,
            [RequireTags("id")] string id,
            [Keywords("{")] string brOpen,
            [Many(true)][RequireTags("method", "field")] string[] members,
            [Keywords("}")] string brClose)
        {
            var head = $"{Space(modifiers)} class {id}".Trim();
            Console.WriteLine($"Read {head} with members: {Comma(members)}");
            return head;
        }

        [SetTag("method")]
        public static string Method(
            [RequireTags("mods")] string[] modifiers,
            [RequireTags("id")] string returnType,
            [RequireTags("id")] string name,
            [Keywords("(")] string brOpen,
            [Optional(false)][RequireTags("method.params")] IEnumerable<string> parameters,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] string statement
            )
        {
            var head = $"{Space(modifiers)} method {name}".Trim();
            Console.WriteLine($"Read {head} returns {returnType} parameters: {Comma(parameters)}; body is {statement}");
            return head;
        }

        [SetTag("method.params")]
        public static IEnumerable<string> MethodParams(
            [Optional(false)][Keywords("ref", "out")] string modifier,
            [RequireTags("id")] string type,
            [RequireTags("id")] string name,
            [Optional(false)][Keywords(",")] string comma,
            [TogetherWith][RequireTags("method.params")] IEnumerable<string> tail
            )
        {
            var head = $"{modifier} parameter {name} typeof {type}".Trim();
            Console.WriteLine($"Read {head}");
            if (comma is null) return new string[] { head };
            return tail.Prepend(head);
        }

        [SetTag("field")]
        public static string Field(
            [RequireTags("mods")] string[] modifiers,
            [RequireTags("id")] string type,
            [RequireTags("id")] string name,
            [Optional(false)][Keywords("=")] string assign,
            [TogetherWith][RequireTags("expression")] string expression,
            [Keywords(";")] string semicolon
            )
        {
            var head = $"{Space(modifiers)} field {name} typeof {type}".Trim();
            var output = $"Read {head}";
            if (assign is not null)
                output += $" initialized with {expression}";
            Console.WriteLine(output);
            return head;
        }

        [SetTag("statement")]
        public static string VarInit(
            [RequireTags("id")] string type,
            [RequireTags("id")] string name,
            [Optional(false)][Keywords("=")] string assign,
            [TogetherWith][RequireTags("expression")] string expression,
            [Keywords(";")] string semicolon
            )
        {
            var head = $"local variable {name} typeof {type}".Trim();
            var output = $"Read {head}";
            if (assign is not null)
                output += $" initialized with {expression}";
            Console.WriteLine(output);
            return head;
        }

        [SetTag("statement")]
        public static string ExpressionAsStatement(
            [RequireTags("expression")] string expression,
            [Keywords(";")] string semicolon
            )
        {
            Console.WriteLine($"Read evaluation of expression {expression}");
            return "expression";
        }

        [SetTag("statement")]
        public static string IfStatement(
            [Keywords("if")] string keyword,
            [Keywords("(")] string brOpen,
            [RequireTags("expression")] string condition,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] string body,
            [Optional(true)][Keywords("else")] string kwelse,
            [TogetherWith][RequireTags("statement")] string elseBranch
            )
        {
            string output = $"if-statement condition: {condition} then: {body}";
            if (kwelse is not null) output += $" else: {elseBranch}";
            Console.WriteLine(output);
            return kwelse is null ? "if-then" : "if-then-else";
        }

        [SetTag("statement")]
        public static string WhileStatement(
            [Keywords("while")] string keyword,
            [Keywords("(")] string brOpen,
            [RequireTags("expression")] string condition,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] string body
            )
        {
            Console.WriteLine($"while-statement condition: {condition} do: {body}");
            return "while-do";
        }

        [SetTag("statement")]
        public static string StatementBlock(
            [Keywords("{")] string brOpen,
            [Many(true)][RequireTags("statement")] string[] statements,
            [Keywords("}")] string brClose
            )
        {
            Console.WriteLine($"statement-block body: {Comma(statements)}");
            return "{}";
        }

        // expressions atom; ++ -- prefix/suffix; +-~! unary; +-*/<>= == != <= >= binary; . as id separator; () callable and group.

        [SetTag("r-expr-A")]
        public static string LiteralOrID(
            [RequireTags("int", "str", "float", "id")] Parsed<string> t            
            )
        {
            string head = t.Tag switch
            {
                "int" => $"integer const {t.Self}",
                "str" => $"string const {t.Self}",
                "float" => $"float const {t.Self}",
                "id" => $"identifier {t.Self}",
                _ => throw new NotImplementedException()
            };
            Console.WriteLine($"Read {head}");
            return head;
        }

        [SetTag("r-expr-A")]
        public static string Dot(
            [RequireTags("r-expr-A")] string obj,
            [Keywords(".")] string dot,
            [RequireTags("id")] string member
            )
        {
            Console.WriteLine($"Read .-expression object: {obj} member: {member}");
            return ".-expr";
        }

        [SetTag("r-expr-A")]
        public static string Call(
            [RequireTags("r-expr-A")] string obj,
            [Keywords("(")] string brOpen,
            [Optional(false)][RequireTags("call.params")] IEnumerable<string> args,
            [Keywords(")")] string brClose
            )
        {
            Console.WriteLine($"Read method calling object: {obj} args: {Comma(args)}");
            return "f(...)-expr";
        }

        [SetTag("call.params")]
        public static IEnumerable<string> CallParams(
            [RequireTags("expression")] string arg,
            [Optional(false)][Keywords(",")] string comma,
            [TogetherWith][RequireTags("call.params")] IEnumerable<string> tail
            )
        {
            if (comma is null) return new string[] { arg };
            return tail.Prepend(arg);
        }

        [SetTag("r-expr-A")]
        public static string Grouping(
            [Keywords("(")] string brOpen,
            [RequireTags("expression")] string expression,
            [Keywords(")")] string brClose
            )
        {
            return expression;
        }

        [SetTag("r-expr-B")]
        public static string PrefixPP(
            [Keywords("++", "--")] string op,
            [RequireTags("l-expr")] string lexpr 
            )
        {
            Console.WriteLine($"Read operation {op}x where x: {lexpr}");
            return $"{op}x-expr";
        }

        [SetTag("r-expr-B")]
        public static string SuffixPP(
            [RequireTags("l-expr")] string lexpr,
            [Keywords("++", "--")] string op
            )
        {
            Console.WriteLine($"Read operation x{op} where x: {lexpr}");
            return $"x{op}-expr";
        }

        [SetTag("r-expr-C")]
        public static string UnaryOperand(
            [RequireTags("r-expr-A", "r-expr-B")] string operand
            )
        {
            return operand;
        }

        [SetTag("r-expr-C")]
        public static string UnaryOperation(
            [Keywords("+", "-", "~", "!")] string operation,
            [RequireTags("r-expr-C")] string operand
            )
        {
            var head = $"unary {operation} expression";
            Console.WriteLine($"Read {head} operand: {operand}");
            return head;
        }

        [SetTag("r-expr-D")]
        public static string OperandD(
            [RequireTags("r-expr-C")] string operand
            )
        {
            return operand;
        }

        [SetTag("r-expr-D")]
        public static string OperationD(
            [RequireTags("r-expr-D")] string left,
            [Keywords("*", "/", "%")] string sign,
            [RequireTags("r-expr-C")] string right
            )
        {
            var head = $"binary {sign} expression";
            Console.WriteLine($"Read {head} left: {left} right: {right}");
            return head;
        }

        [SetTag("r-expr-E")]
        public static string OperandE(
            [RequireTags("r-expr-D")] string operand
            )
        {
            return operand;
        }

        [SetTag("r-expr-E")]
        public static string OperationE(
            [RequireTags("r-expr-E")] string left,
            [Keywords("+", "-")] string sign,
            [RequireTags("r-expr-D")] string right
            )
        {
            var head = $"binary {sign} expression";
            Console.WriteLine($"Read {head} left: {left} right: {right}");
            return head;
        }

        [SetTag("r-expr-F")]
        public static string OperandF(
            [RequireTags("r-expr-E")] string operand
            )
        {
            return operand;
        }

        [SetTag("r-expr-F")]
        public static string OperationF(
            [RequireTags("r-expr-F")] string left,
            [Keywords("==", "!=", "<", ">", "<=", ">=")] string sign,
            [RequireTags("r-expr-E")] string right
            )
        {
            var head = $"binary {sign} expression";
            Console.WriteLine($"Read {head} left: {left} right: {right}");
            return head;
        }

        [SetTag("expression")]
        public static string ExpressionAsF(
            [RequireTags("r-expr-F")] string operand
            )
        {
            return operand;
        }

        [SetTag("expression")]
        public static string Assign(
            [RequireTags("l-expr")] string left,
            [Keywords("=")] string sign,
            [RequireTags("expression")] string right
            )
        {
            var head = $"binary {sign} expression";
            Console.WriteLine($"Read {head} left: {left} right: {right}");
            return head;
        }

        [SetTag("l-expr")]
        public static string IDLikeVar(
            [RequireTags("id")] string id
            )
        {
            return id;
        }

        // end of grammar
        public static void TestSyntax()
        {
            try
            {
                Console.WriteLine("Output: " +
                new ParsingEngineBuilder()
                .AddToken("id", @"[[:alpha:]_][[:alnum:]_]*")
                .AddToken("int", @"[1-9][0-9]*|0")
                .AddToken("str", @"""[:print:]""")
                .AddToken("float", @"([1-9][0-9]*|0)\.[0-9]*|\.[0-9]+")
                .AddToken(SpecialTags.TAG_SKIP, "[[:space:]]")
                .AddProductions<SyntaxTester>()
                .Create("program")
                .ParseFile<object>("input.txt"));
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
