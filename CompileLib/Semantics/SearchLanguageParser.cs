﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Parsing;

#warning NEED TO BE FIXED
namespace CompileLib.Semantics
{
    internal class SearchLanguageParser
    {
        [SetTag("program")]
        public static SearchEngine ReadProgram(
            [Many(false)][RequireTags("func")] SearchFunction[] functions
            )
        {
            SearchEngine result = new();
            foreach (var func in functions)
                if(!result.AddRule(func))
                {
                    throw new SearchLangParsingException($"Duplicate function {func.Name}", func.Line, func.Column);
                }
            result.Check();
            return result;
        }

        [SetTag("func")]
        public static SearchFunction ReadFunction(
            [RequireTags("var")] Parsed<string> name,
            [Keywords("(")] string brOpen,
            [Optional(false)][RequireTags("func.params")] List<Parsed<string>>? parameters,
            [Keywords(")")] string brClose,
            [Keywords("=")] string assign,
            [RequireTags("expr")] SearchRule expr,
            [Keywords(";")] string semicolon
            )
        {
            string[] paramArray;
            if(parameters is null)
            {
                paramArray = Array.Empty<string>();
            }
            else
            {
                parameters.Reverse();
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters.Take(i).Any(p => p.Self == parameters[i].Self))
                        throw new SearchLangParsingException("Duplicate param name", parameters[i].Line, parameters[i].Column);
                paramArray = parameters.Select(p => p.Self).ToArray();
            }
            return new SearchFunction(expr, name.Self, name.Line, name.Column, paramArray);
        }

        [SetTag("func.params")]
        public static List<Parsed<string>> ReadParams(
            [RequireTags("var")] Parsed<string> param,
            [Optional(false)][Keywords(",")] string separator,
            [TogetherWith][RequireTags("func.params")] List<Parsed<string>> tail
            )
        {
            if(separator is null) return new List<Parsed<string>> { param };
            tail.Add(param);
            return tail;
        }

        [SetTag("atom-func")]
        public static Parsed<string> ReadAtomFunc(
            [Keywords("name", "type", "relation", "attribute")] Parsed<string> func
            )
        {
            return func;
        }

        [SetTag("expr-A")]
        public static SearchRule ReadCall(
            [RequireTags("var", "atom-func")] Parsed<string> name,
            [Keywords("(")] string brOpen,
            [Optional(false)][RequireTags("call-args")] List<string>? parameters,
            [Keywords(")")] string brClose
            )
        {
            string[] paramArray;
            if (parameters is null)
            {
                paramArray = Array.Empty<string>();
            }
            else
            {
                parameters.Reverse();
                paramArray = parameters.ToArray();
            }
            return new SearchRuleCall(name.Line, name.Column, name.Self, paramArray);
        }

        [SetTag("call-args")]
        public static List<string> ReadArgs(
            [RequireTags("var", "atom")] string param,
            [Optional(false)][Keywords(",")] string separator,
            [TogetherWith][RequireTags("call-args")] List<string> tail
            )
        {
            if(param.StartsWith('"')) param = param[1..^1];
            if (separator is null) return new List<string> { param };
            tail.Add(param);
            return tail;
        }

        [SetTag("expr-A")]
        public static SearchRule ReadExprInBrackets(
            [Keywords("(")] Parsed<string> brOpen,
            [RequireTags("expr")] SearchRule expr,
            [Keywords(")")] string brClose
            )
        {
            return expr.ChangePos(brOpen.Line, brOpen.Column);
        }

        [SetTag("expr-B")]
        public static SearchRule ConvertA2B(
            [RequireTags("expr-A")] SearchRule expr
            )
        {
            return expr;
        }

        [SetTag("expr-B")]
        public static SearchRule ReadIntersection(
            [RequireTags("expr-B")] SearchRule left,
            [Keywords("&")] string op,
            [RequireTags("expr-A")] SearchRule right
            )
        {
            return new SearchRuleIntersection(left, right);
        }

        [SetTag("expr-C")]
        public static SearchRule ConvertB2C(
            [RequireTags("expr-B")] SearchRule expr
            )
        {
            return expr;
        }

        [SetTag("expr-C")]
        public static SearchRule ReadChain(
            [RequireTags("expr-C")] SearchRule left,
            [Keywords(".", "+.")] string op,
            [RequireTags("expr-B")] SearchRule right
            )
        {
            return new ChainSearchRule(left, right, op == "+.");
        }

        [SetTag("expr")]
        public static SearchRule ConvertC2Expr(
            [RequireTags("expr-C")] SearchRule expr
            )
        {
            return expr;
        }

        [SetTag("expr")]
        public static SearchRule ReadUnion(
            [RequireTags("expr")] SearchRule left,
            [Keywords("|", "?|")] string op,
            [RequireTags("expr-C")] SearchRule right
            )
        {
            return new SearchRuleUnion(left, right, op == "?|");
        }

        private readonly ParsingEngine SLParsingEngine = new ParsingEngineBuilder()
            .AddToken("atom", @"[-[:alnum:]_]+|""[^""[:cntrl:]]*""")
            .AddToken("var", @"@[-[:alnum:]_]+")
            .AddToken(SpecialTags.TAG_SKIP, "[[:space:]]")
            .AddToken(SpecialTags.TAG_SKIP, @"#[^[:cntrl:]]*")
            .AddProductions<SearchLanguageParser>()
            .Create("program");

        private static SearchLanguageParser? _instance;
        public static SearchLanguageParser Instance => _instance ??= new();

        public SearchEngine Parse(string code)
        {
            SearchEngine engine = new();
            try
            {
                return SLParsingEngine.Parse<SearchEngine>(code).Self;
            }
            catch (AnalysisStopException e)
            {
                throw new SearchLangParsingException("Error parsing SearchLang code", e.Token.Line, e.Token.Column);
            }
        }
    }
}
