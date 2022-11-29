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
        public class SignatureStart
        {
            public Parsed<string>? visMod;
            public Parsed<string>? kwStatic;
            public TypeExpression type;
            public string id;
        }

        [SetTag("global")]
        public static GlobalScope ReadGlobal(
            [Many(true)][RequireTags("class")] Parsed<Class>[] components,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                ErrorList.AddUnexpected(e.NextToken);
                e.Skip();
            }

            foreach(var p in components)
            {
                Console.WriteLine($"Class at {p.Line} {p.Column} having tag {p.Tag}");
            }

            //return new GlobalScope(components);
            return new GlobalScope(Array.ConvertAll(components, p => p.Self));
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

        public static ErrorList ErrorList = new();

        [SetTag("type-expr")]
        public static TypeExpression? ReadTypeExpression(
            [RequireTags("id")] Parsed<string> className,
            [Many(true)][Keywords("[")] string[] brOpen,
            [TogetherWith][Keywords("]")] string[] brClose,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if (className is null)
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                else if (brOpen.Length == brClose.Length)
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                else if(brOpen.Length > brClose.Length)
                {
                    ErrorList.AddExpectation("']'", tk.Line, tk.Column);
                    e.PerformBefore(new(SpecialTags.TAG_KEYWORD, "]", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new TypeExpression(className.Line, className.Column, className.Self, brOpen.Length);
        }

        [SetTag("class")]
        public static Class? ReadClass(
            [Keywords("class")] Parsed<string> kw,
            [RequireTags("id")] string id,
            [Optional(false)][Keywords(":")] string inherited,
            [TogetherWith][RequireTags("id")] string baseClass,
            [Keywords("{")] string brOpen,
            [Many(true)][RequireTags("method", "field")] ClassMember[] components,
            [Keywords("}")] string brClose,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if (kw is null)
                {
                    ErrorList.AddExpectation("Keyword 'class'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "class", tk.Line, tk.Column));
                }
                else if(id is null)
                {
                    ErrorList.AddExpectation("Identifier", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("id", "<unnamed>", tk.Line, tk.Column));
                }
                else if(inherited is not null && baseClass is null)
                {
                    ErrorList.AddExpectation("Identifier", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("id", "<unknown>", tk.Line, tk.Column));
                }
                else if(brOpen is null)
                {
                    ErrorList.AddExpectation("'{'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "{", tk.Line, tk.Column));
                }
                else if(brClose is null)
                {
                    ErrorList.AddExpectation("'}'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "}", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new Class(id, kw.Line, kw.Column, baseClass, components);
        }

        [SetTag("visibility-mod")]
        public static Parsed<string>? ReadVisibilityModifier(
            [Keywords("public", "private", "protected")] Parsed<string> self,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                ErrorList.AddUnexpected(e.NextToken);
                e.Skip();
                return null;
            }

            return self;
        }

        [SetTag("signature-start")]
        public static SignatureStart? ReadSignatureStart(
            [Optional(false)][RequireTags("visibility-mod")] Parsed<string>? visMod,
            [Optional(false)][Keywords("static")] Parsed<string>? kwStatic,
            [RequireTags("type-expr")] TypeExpression type,
            [RequireTags("id")] string id,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(type is null)
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                else if(id is null)
                {
                    ErrorList.AddExpectation("Id", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("id", "<unnamed>", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new SignatureStart
            {
                visMod = visMod,
                kwStatic = kwStatic,
                type = type,
                id = id
            };
        }

        [SetTag("field")]
        public static ClassMember? ReadField(
            [RequireTags("signature-start")] SignatureStart s,
            [Keywords(";")] string close,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(s is null)
                {
                    ErrorList.AddUnexpected(e.NextToken);
                    e.Skip();
                }
                else if(close is null)
                {
                    ErrorList.AddExpectation("';'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            var firstToken = s.visMod ?? s.kwStatic;
            return new Field(s.id, firstToken?.Line ?? s.type.Line, firstToken?.Column ?? s.type.Column, Str2Visibility(s.visMod?.Self), s.kwStatic is not null, s.type);
        }

        [SetTag("method")]
        public static ClassMember? ReadMethod(
            [RequireTags("signature-start")] SignatureStart s,
            [Keywords("(")] string brOpen,
            [Optional(false)][RequireTags("method.params")] List<Parameter> parameters,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] Statement statement,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (s is null)
                {
                    ErrorList.AddUnexpected(e.NextToken);
                    e.Skip();
                }
                else if (brOpen is null)
                {
                    ErrorList.AddExpectation("'('", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "(", tk.Line, tk.Column));
                }
                else if(brClose is null)
                {
                    ErrorList.AddExpectation("')'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ")", tk.Line, tk.Column));
                }
                else if(statement is null)
                {
                    ErrorList.AddUnexpected(e.NextToken);
                    e.Skip();
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            var firstToken = s.visMod ?? s.kwStatic;

            Parameter[] paramArray;
            if(parameters is null) paramArray = Array.Empty<Parameter>();
            else
            {
                parameters.Reverse();
                paramArray = parameters.ToArray();
            }
            return new Method(s.id, firstToken?.Line ?? s.type.Line, firstToken?.Column ?? s.type.Column, Str2Visibility(s.visMod?.Self), s.kwStatic is not null, s.type, statement, paramArray);
        }

        [SetTag("method.params")]
        public static List<Parameter>? ReadMethodParameters(
            [RequireTags("type-expr")] TypeExpression type,
            [RequireTags("id")] string id,
            [Optional(false)][Keywords(",")] string separator,
            [TogetherWith][RequireTags("method.params")] List<Parameter>? tail,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(type is null)
                {
                    ErrorList.AddUnexpected(e.NextToken);
                    e.Skip();
                }
                else if(id is null)
                {
                    ErrorList.AddExpectation("Identifier", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("id", "<unnamed>", tk.Line, tk.Column));
                }
                else if(separator is not null && (tail is null || tail.Count == 0))
                {
                    ErrorList.AddUnexpected(e.NextToken);
                    e.Skip();
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            Parameter parameter = new(id, type.Line, type.Column, type);
            if(tail is null) return new List<Parameter> { parameter };
            tail.Add(parameter);
            return tail;
        }

        [SetTag("statement")]
        public static Statement? ReadInitStatement(
            [Keywords("var")] Parsed<string> kwvar,
            [RequireTags("type-expr")] TypeExpression type,
            [RequireTags("id")] string id,
            [Optional(false)][Keywords("=")] string eq,
            [TogetherWith][RequireTags("expr")] Expression expr,
            [Keywords(";")] string close,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(kwvar is null)
                {
                    ErrorList.AddExpectation("Keyword 'var'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "var", tk.Line, tk.Column));
                }
                else if(type is null)
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                else if(id is null)
                {
                    ErrorList.AddExpectation("Identifier", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("id", "<unnamed>", tk.Line, tk.Column));
                }
                else if(eq is not null && expr is null)
                {
                    ErrorList.AddExpectation("Expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else if(close is null)
                {
                    ErrorList.AddExpectation("';'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            LocalVariable? localVariable = new(id, type.Line, type.Column, type);
            return new SimpleStatement(kwvar.Line, kwvar.Column, localVariable, expr);
        }

        [SetTag("statement")]
        public static Statement? ReadSimpleStatement(
            [Optional(false)][RequireTags("expr")] Expression expr,
            [Keywords(";")] Parsed<string> close,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(close is null)
                {
                    ErrorList.AddExpectation("';'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new SimpleStatement(expr?.Line ?? close.Line, expr?.Column ?? close.Column, null, expr);
        }

        [SetTag("statement")]
        public static Statement? ReadStatementBlock(
            [Keywords("{")] Parsed<string> brOpen,
            [Many(true)][RequireTags("statement")] Statement[] statements,
            [Keywords("}")] string brClose,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(brOpen is null)
                {
                    ErrorList.AddExpectation("'{'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "{", tk.Line, tk.Column));
                }
                else if(brClose is null)
                {
                    ErrorList.AddExpectation("'}'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "}", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new BlockStatement(brOpen.Line, brOpen.Column, statements);
        }

        [SetTag("statement")]
        public static Statement? ReadIfStatement(
            [Keywords("if")] Parsed<string> kw,
            [Keywords("(")] string brOpen,
            [RequireTags("expr")] Expression condition,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] Statement ifBranch,
            [Optional(true)][Keywords("else")] string kwElse,
            [TogetherWith][RequireTags("statement")] Statement elseBranch,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(kw is null)
                {
                    ErrorList.AddExpectation("Keyword 'if'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "if", tk.Line, tk.Column));
                }
                else if(brOpen is null)
                {
                    ErrorList.AddExpectation("'('", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "(", tk.Line, tk.Column));
                }
                else if(condition is null)
                {
                    ErrorList.AddExpectation("Condition expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else if(brClose is null)
                {
                    ErrorList.AddExpectation("')'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ")", tk.Line, tk.Column));
                }
                else if(ifBranch is null)
                {
                    ErrorList.AddExpectation("If-branch statement", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else if(kwElse is not null && elseBranch is null)
                {
                    ErrorList.AddExpectation("Else-branch statement", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }

                return null;
            }

            return new IfStatement(kw.Line, kw.Column, condition, ifBranch, elseBranch);
        }

        [SetTag("statement")]
        public static Statement? ReadWhileStatement(
            [Keywords("while")] Parsed<string> kw,
            [Keywords("(")] string brOpen,
            [RequireTags("expr")] Expression condition,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] Statement body,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (kw is null)
                {
                    ErrorList.AddExpectation("Keyword 'while'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "while", tk.Line, tk.Column));
                }
                else if (brOpen is null)
                {
                    ErrorList.AddExpectation("'('", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "(", tk.Line, tk.Column));
                }
                else if (condition is null)
                {
                    ErrorList.AddExpectation("Condition expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else if (brClose is null)
                {
                    ErrorList.AddExpectation("')'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ")", tk.Line, tk.Column));
                }
                else if (body is null)
                {
                    ErrorList.AddExpectation("While loop body", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }

                return null;
            }

            return new WhileStatement(kw.Line, kw.Column, condition, body, false);
        }

        [SetTag("statement")]
        public static Statement? ReadDoWhileStatement(
            [Keywords("do")] Parsed<string> kw,
            [RequireTags("statement")] Statement body,
            [Keywords("while")] string kwWhile,
            [Keywords("(")] string brOpen,
            [RequireTags("expr")] Expression condition,
            [Keywords(")")] string brClose,
            [Keywords(";")] string close,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (kw is null)
                {
                    ErrorList.AddExpectation("Keyword 'do'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "do", tk.Line, tk.Column));
                }
                else if (body is null)
                {
                    ErrorList.AddExpectation("Do-While loop body", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else if (kwWhile is null)
                {
                    ErrorList.AddExpectation("Keyword 'while'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "while", tk.Line, tk.Column));
                }
                else if (brOpen is null)
                {
                    ErrorList.AddExpectation("'('", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "(", tk.Line, tk.Column));
                }
                else if (condition is null)
                {
                    ErrorList.AddExpectation("Condition expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else if (brClose is null)
                {
                    ErrorList.AddExpectation("')'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ")", tk.Line, tk.Column));
                }
                else if (brClose is null)
                {
                    ErrorList.AddExpectation("';'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }

                return null;
            }

            return new WhileStatement(kw.Line, kw.Column, condition, body, true);
        }

        [SetTag("statement")]
        public static Statement? ReadForStatement(
            [Keywords("for")] Parsed<string> kw,
            [Keywords("(")] string brOpen,
            [RequireTags("statement")] Statement init,
            [RequireTags("expr")] Expression cond,
            [Keywords(";")] string sep2,
            [RequireTags("statement")] Statement step,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] Statement body,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (kw is null)
                {
                    ErrorList.AddExpectation("Keyword 'for'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "for", tk.Line, tk.Column));
                }
                else if (brOpen is null)
                {
                    ErrorList.AddExpectation("'('", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "(", tk.Line, tk.Column));
                }
                else if (init is null)
                {
                    ErrorList.AddExpectation("For loop init statement", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else if (cond is null)
                {
                    ErrorList.AddExpectation("Condition expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else if (sep2 is null)
                {
                    ErrorList.AddExpectation("';'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else if (step is null)
                {
                    ErrorList.AddExpectation("For loop step statement", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else if (brClose is null)
                {
                    ErrorList.AddExpectation("')'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ")", tk.Line, tk.Column));
                }
                else if (body is null)
                {
                    ErrorList.AddExpectation("For loop body", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }

                return null;
            }

            return new ForStatement(kw.Line, kw.Column, init, cond, step, body);
        }

        [SetTag("statement")]
        public static Statement? ReadReturn(
            [Keywords("return")] Parsed<string> kw,
            [Optional(false)][RequireTags("expr")] Expression expr,
            [Keywords(";")] string close,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(kw is null)
                {
                    ErrorList.AddExpectation("Keyword 'return'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "return", tk.Line, tk.Column));
                }
                else if(close is null)
                {
                    ErrorList.AddExpectation("';'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new ReturnStatement(kw.Line, kw.Column, expr);
        }

        [SetTag("statement")]
        public static Statement? ReadBreakContinue(
            [Keywords("break", "continue")] Parsed<string> kw,
            [Keywords(";")] string close,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(kw is null)
                {
                    ErrorList.AddExpectation("Keyword 'return' or 'break'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "return", tk.Line, tk.Column));
                }
                else if (close is null)
                {
                    ErrorList.AddExpectation("';'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ";", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new BreakContinueStatement(kw.Line, kw.Column, kw.Self == "break");
        }

        [SetTag("call-args")]
        public static List<Expression>? ReadCallArgs(
            [RequireTags("expr")] Expression expr,
            [Optional(false)][Keywords(",")] string separator,
            [TogetherWith][RequireTags("call-args")] List<Expression> tail,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(expr is null)
                {
                    ErrorList.AddExpectation("Expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else if(separator is not null && (tail is null || tail.Count == 0))
                {
                    ErrorList.AddExpectation("Expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }

                return null;
            }

            if (tail is null) return new List<Expression> { expr };
            tail.Add(expr);
            return tail;
        }

        [SetTag("expr-atom")]
        public static Expression? ReadExprInBrackets(
            [Keywords("(")] Parsed<string> brOpen,
            [RequireTags("expr")] Expression expr,
            [Keywords(")")] string brClose,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(brOpen is null)
                {
                    e.NextHandler();
                }
                else if(expr is null)
                {
                    ErrorList.AddExpectation("Expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else if(brClose is null)
                {
                    ErrorList.AddExpectation("')'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ")", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            expr.ChangePosition(brOpen.Line, brOpen.Column);
            return expr;
        }

        [SetTag("expr-atom")]
        public static Expression? ReadNewOrThis(
            [Keywords("new", "this")] Parsed<string> id,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(id is null)
                {
                    e.NextHandler();
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new IdExpression(id.Self, id.Line, id.Column);
        }

        [SetTag("expr-atom")]
        public static Expression? ReadID(
            [RequireTags("id")] Parsed<string> id,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (id is null)
                {
                    ErrorList.AddExpectation("Expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("id", "<unknown>", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new IdExpression(id.Self, id.Line, id.Column);
        }

        [SetTag("expr-atom")]
        public static Expression? ReadConst(
            [RequireTags("int10", "int16", "int8", "int2", "str", "char", "float")] Parsed<string> token,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (token is null)
                {
                    e.NextHandler();
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            if(token.Tag == "[helper tag]43")
            {
                Console.WriteLine("here");
            }
            return new ConstExpression(token.Self, token.Tag, token.Line, token.Column);
        }

        [SetTag("expr-call")]
        public static Expression? ConvertAtom2Call(
            [RequireTags("expr-atom")] Expression expr,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(expr is null)
                {
                    e.NextHandler();
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return expr;
        }

        [SetTag("expr-call")]
        public static Expression? ReadRoundCall(
            [RequireTags("expr-call")] Expression callee,
            [Keywords("(")] string brOpen,
            [Optional(false)][RequireTags("call-args")] List<Expression> args,
            [Keywords(")")] string brClose,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(callee is null)
                {
                    e.NextHandler();
                }
                else if(brOpen is null)
                {
                    ErrorList.AddExpectation("'('", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "(", tk.Line, tk.Column));
                }
                else if(brClose is null)
                {
                    ErrorList.AddExpectation("')'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ")", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            Expression[] argsArray;
            if(args is null) argsArray = Array.Empty<Expression>();
            else
            {
                args.Reverse();
                argsArray = args.ToArray();
            }
            return new CallExpression(callee, argsArray, brOpen, callee.Line, callee.Column);
        }

        [SetTag("expr-call")]
        public static Expression? ReadSquareCall(
            [RequireTags("expr-call")] Expression callee,
            [Keywords("[")] string brOpen,
            [Optional(false)][RequireTags("call-args")] List<Expression> args,
            [Keywords("]")] string brClose,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (callee is null)
                {
                    e.NextHandler();
                }
                else if (brOpen is null)
                {
                    ErrorList.AddExpectation("'('", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "(", tk.Line, tk.Column));
                }
                else if (brClose is null)
                {
                    ErrorList.AddExpectation("')'", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ")", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            Expression[] argsArray;
            if (args is null) argsArray = Array.Empty<Expression>();
            else argsArray = args.ToArray();
            return new CallExpression(callee, argsArray, brOpen, callee.Line, callee.Column);
        }

        [SetTag("expr-call")]
        public static Expression? ReadDotExpression(
            [RequireTags("expr-call")] Expression left,
            [Keywords(".")] string operation,
            [RequireTags("id")] string id,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if(e is not null)
            {
                var tk = e.NextToken;
                if(left is null)
                {
                    e.NextHandler();
                }
                else if(operation is null)
                {
                    ErrorList.AddExpectation("Operation sign", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, ".", tk.Line, tk.Column));
                }
                else if(id is null)
                {
                    ErrorList.AddExpectation("Identifier", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("id", "<unnamed>", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new DotExpression(left, id, operation, left.Line, left.Column);
        }

        [SetTag("expr-unary")]
        public static Expression? ConvertCall2Unary(
            [RequireTags("expr-call")] Expression expr,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (expr is null)
                {
                    e.NextHandler();
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return expr;
        }

        [SetTag("expr-unary")]
        public static Expression? ReadUnaryExpression(
            [Keywords("!", "~", "-", "+", "*", "&")] Parsed<string> operation,
            [RequireTags("expr-unary")] Expression operand,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (operation is null)
                {
                    e.NextHandler();
                }
                else if (operand is null)
                {
                    ErrorList.AddExpectation("Expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new UnaryExpression(operand, operation.Self, operation.Line, operation.Column);
        }

        [SetTag("expr-mul")]
        public static Expression? ConvertUnary2Mul(
            [RequireTags("expr-unary")] Expression expr,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (expr is null)
                {
                    e.NextHandler();
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return expr;
        }

        [SetTag("expr-mul")]
        public static Expression? ReadMulExpression(
            [RequireTags("expr-mul")] Expression left,
            [Keywords("*", "/", "%")] Parsed<string> operation,
            [RequireTags("expr-unary")] Expression right,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (left is null)
                {
                    e.NextHandler();
                }
                else if (operation is null)
                {
                    ErrorList.AddExpectation("Operation sign", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "*", tk.Line, tk.Column));
                }
                else if (right is null)
                {
                    ErrorList.AddExpectation("Expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new BinaryExpression(left, right, operation.Self, left.Line, left.Column);
        }

        [SetTag("expr-add")]
        public static Expression? ConvertMul2Add(
            [RequireTags("expr-mul")] Expression expr,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (expr is null)
                {
                    e.NextHandler();
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return expr;
        }

        [SetTag("expr-add")]
        public static Expression? ReadAddExpression(
            [RequireTags("expr-add")] Expression left,
            [Keywords("+", "-")] Parsed<string> operation,
            [RequireTags("expr-mul")] Expression right,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (left is null)
                {
                    e.NextHandler();
                }
                else if (operation is null)
                {
                    ErrorList.AddExpectation("Operation sign", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "+", tk.Line, tk.Column));
                }
                else if (right is null)
                {
                    ErrorList.AddExpectation("Expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new BinaryExpression(left, right, operation.Self, left.Line, left.Column);
        }

        [SetTag("expr-cmp")]
        public static Expression? ConvertAdd2Cmp(
            [RequireTags("expr-add")] Expression expr,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (expr is null)
                {
                    e.NextHandler();
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return expr;
        }

        [SetTag("expr-cmp")]
        public static Expression? ReadCmpExpression(
            [RequireTags("expr-cmp")] Expression left,
            [Keywords("==", "!=", "<", ">", "<=", ">=")] Parsed<string> operation,
            [RequireTags("expr-add")] Expression right,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (left is null)
                {
                    e.NextHandler();
                }
                else if (operation is null)
                {
                    ErrorList.AddExpectation("Operation sign", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "*", tk.Line, tk.Column));
                }
                else if (right is null)
                {
                    ErrorList.AddExpectation("Expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new BinaryExpression(left, right, operation.Self, left.Line, left.Column);
        }

        [SetTag("expr-log")]
        public static Expression? ConvertCmp2Log(
            [RequireTags("expr-cmp")] Expression expr,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (expr is null)
                {
                    e.NextHandler();
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return expr;
        }

        [SetTag("expr-log")]
        public static Expression? ReadLogExpression(
            [RequireTags("expr-log")] Expression left,
            [Keywords("&&", "||", "&", "|", "^", "<<", ">>")] Parsed<string> operation,
            [RequireTags("expr-cmp")] Expression right,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (left is null)
                {
                    e.NextHandler();
                }
                else if (operation is null)
                {
                    ErrorList.AddExpectation("Operation sign", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "&", tk.Line, tk.Column));
                }
                else if (right is null)
                {
                    ErrorList.AddExpectation("Expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new BinaryExpression(left, right, operation.Self, left.Line, left.Column);
        }

        [SetTag("expr")]
        public static Expression? ConvertLog2Assign(
            [RequireTags("expr-log")] Expression expr,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (expr is null)
                {
                    e.NextHandler();
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return expr;
        }

        [SetTag("expr")]
        public static Expression? ReadAssignExpression(
            [RequireTags("expr-log")] Expression left,
            [Keywords("=")] Parsed<string> operation,
            [RequireTags("expr")] Expression right,
            [ErrorHandler] ErrorHandlingDecider e
            )
        {
            if (e is not null)
            {
                var tk = e.NextToken;
                if (left is null)
                {
                    e.NextHandler();
                }
                else if (operation is null)
                {
                    ErrorList.AddExpectation("Operation sign", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>(SpecialTags.TAG_KEYWORD, "=", tk.Line, tk.Column));
                }
                else if (right is null)
                {
                    ErrorList.AddExpectation("Expression", tk.Line, tk.Column);
                    e.PerformBefore(new Parsed<string>("int10", "0", tk.Line, tk.Column));
                }
                else
                {
                    ErrorList.AddUnexpected(tk);
                    e.Skip();
                }
                return null;
            }

            return new BinaryExpression(left, right, operation.Self, left.Line, left.Column);
        }
    }
}
