using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using CompileLib.LexerTools;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Parser of POSIX-regexps
    /// </summary>
    internal class RegexParser
    {
        private class Expr
        {
            // whole expression
            [SetTag("expr")]
            public static SmartFSMBuilder MakeExpression(
                [Optional(false)][RequireTags("expr")] SmartFSMBuilder expr,
                [TogetherWith][Keywords("|")] string _,
                [RequireTags("or-branch")] SmartFSMBuilder branch
                )
            {
                if (expr is null)
                    return branch;
                else
                    return SmartFSMBuilder.CreateUnion(expr, branch);
            }

            [SetTag("expr-inbr")]
            public static SmartFSMBuilder MakeExpression_Inbr(
                [Optional(false)][RequireTags("expr-inbr")] SmartFSMBuilder expr,
                [TogetherWith][Keywords("|")] string _,
                [RequireTags("or-branch-inbr")] SmartFSMBuilder branch
                )
            {
                return MakeExpression(expr, _, branch);
            }

            [SetTag("or-branch")]
            public static SmartFSMBuilder OrBranch(
                [Many(false)][RequireTags("or-branch-item")] SmartFSMBuilder[] items)
            {
                return items.Aggregate(SmartFSMBuilder.CreateConcatenation);
            }

            [SetTag("or-branch-inbr")]
            public static SmartFSMBuilder OrBranch_Inbr(
                [Many(false)][RequireTags("or-branch-item-inbr")] SmartFSMBuilder[] items)
            {
                return OrBranch(items);
            }

            [SetTag("or-branch-item")]
            public static SmartFSMBuilder Common2OrBranch([RequireTags("or-branch-item-common")] SmartFSMBuilder item)
                => item;

            [SetTag("or-branch-item-inbr")]
            public static SmartFSMBuilder Common2OrBranch_Inbr([RequireTags("or-branch-item-common")] SmartFSMBuilder item)
                => item;

            [SetTag("or-branch-item-common")]
            public static SmartFSMBuilder OrBranchItem_brackets(
                [Keywords("(")] string brOpen,
                [RequireTags("expr-inbr")] SmartFSMBuilder expression,
                [Keywords(")")] string brClose)
            {
                return expression;
            }

            [SetTag("or-branch-item-common")]
            public static SmartFSMBuilder OrBranchItem_nonSpec(
                [RequireTags("non-spec", "digit")] string s)
            {
                char it = s[0];
                return SmartFSMBuilder.CreateBySinglePredicate(c => c == it);
            }

            [SetTag("or-branch-item-common")]
            public static SmartFSMBuilder QuotedChar(
                [Keywords("\\")] string backslash,
                [Keywords("^", ".", "[", "]", "$", "(", ")", "|", "*", "+", "?", "{", "}", "\\")] string s
                )
            {
                char it = s[0];
                return SmartFSMBuilder.CreateBySinglePredicate(c => c == it);
            }

            [SetTag("or-branch-item")]
            public static SmartFSMBuilder Spec(
                [Keywords(".", "]", "}", "^", "$", "-", ":", ",", ")")] string s)
            {
                char it = s[0];
                if (it == '.')
                    return SmartFSMBuilder.CreateBySinglePredicate(c => c != 0);
                else
                    return SmartFSMBuilder.CreateBySinglePredicate(c => c == it);
            }

            [SetTag("or-branch-item-inbr")]
            public static SmartFSMBuilder Spec_Inbr(
                [Keywords(".", "]", "}", "^", "$", "-", ":", ",")] string s)
            {
                return Spec(s);
            }

            [SetTag("or-branch-item-common")]
            public static SmartFSMBuilder BracketExpr(
                [Keywords("[")] string brOpen,
                [Optional(true)][Keywords("^")] string circumflex,
                [Optional(true)][Keywords("]")] string brMember,
                [Many(false)][RequireTags("bracket-expr-item")] Predicate<char>[] items,
                [Keywords("]")] string brClose
                )
            {
                if (brMember is not null) items = items.Prepend(c => c == ']').ToArray();

                return SmartFSMBuilder.CreateBySinglePredicate(
                    circumflex is null
                    ? new Predicate<char>(c => items.Any(p => p(c)))
                    : new Predicate<char>(c => items.All(p => !p(c))));
            }

            [SetTag("bracket-expr-item-simple")]
            public static string BracketExprItem(
                [RequireTags("non-spec", "digit")] string operand)
            {
                return operand;
            }

            [SetTag("bracket-expr-item-simple")]
            public static string BracketExprSpec(
                [Keywords(".", "\\", "(", ")", "*", "+", "?", "{", "}", "|", "$", ":", ",", "^", "-")] string s)
            {
                return s;
            }

            internal static IDictionary<string, Predicate<char>> charClasses;

            [SetTag("bracket-expr-item")]
            public static Predicate<char> CharClass(
                [Keywords("[")] string sqBrOpen,
                [Optional(true)][Keywords(":")] string colon1,
                [TogetherWith][RequireTags("char-class-name")] Token[] name,
                [TogetherWith][Keywords(":")] string colon2,
                [TogetherWith][Keywords("]")] string sqBrClose)
            {
                if (colon1 is null)
                    return c => c == '[';
                string s = new(name.SelectMany(e => e.Self).ToArray());
                if (charClasses.ContainsKey(s))
                    return charClasses[s];
                else
                    throw new RegexParsingException("Invalid char class", name[0].Line, name[0].Column);
            }

            [SetTag("char-class-name")]
            public static Token[] CharClassName([Many(false)][RequireTags("non-spec", "digit")] Token[] name)
            {
                return name;
            }

            [SetTag("bracket-expr-item")]
            public static Predicate<char> Range(
                [RequireTags("bracket-expr-item-simple")] string start,
                [Optional(true)][Keywords("-")] string _,
                [TogetherWith][RequireTags("bracket-expr-item-simple")] string end
                )
            {
                char startChar = start[0];
                if(_ is null)
                {
                    return c => c == startChar;
                }

                char endChar = end[0];
                return c => startChar <= c && c <= endChar;
            }

            [SetTag("or-branch-item")]
            public static SmartFSMBuilder Closure(
                [RequireTags("or-branch-item")] SmartFSMBuilder operand,
                [Keywords("*", "+", "?")] string sign)
            {
                switch(sign[0])
                {
                    case '*':
                        return SmartFSMBuilder.CreateStarClosure(operand);
                    case '+':
                        return SmartFSMBuilder.CreatePlusClosure(operand);
                    case '?':
                        return SmartFSMBuilder.CreateOptional(operand);
                    default:
                        Debug.Fail("Invalid sign");
                        return SmartFSMBuilder.CreateOptional(operand);
                }
            }

            [SetTag("or-branch-item-inbr")]
            public static SmartFSMBuilder Closure_Inbr(
                [RequireTags("or-branch-item-inbr")] SmartFSMBuilder operand,
                [Keywords("*", "+", "?")] string sign)
            {
                return Closure(operand, sign);
            }

            [SetTag("or-branch-item")]
            public static SmartFSMBuilder Interval(
                [RequireTags("or-branch-item")] SmartFSMBuilder operand,
                [Keywords("{")] string brOpen,
                [Many(false)][RequireTags("digit")] string[] count,
                [Keywords("}")] string brClose)
            {
                return SmartFSMBuilder.CreateSimpleDup(operand, int.Parse(count.SelectMany(e => e).ToArray()));
            }

            [SetTag("or-branch-item-inbr")]
            public static SmartFSMBuilder Interval_Inbr(
                [RequireTags("or-branch-item-inbr")] SmartFSMBuilder operand,
                [Keywords("{")] string brOpen,
                [Many(false)][RequireTags("digit")] string[] count,
                [Keywords("}")] string brClose)
            {
                return Interval(operand, brOpen, count, brClose);
            }

            [SetTag("or-branch-item")]
            public static SmartFSMBuilder Interval(
                [RequireTags("or-branch-item")] SmartFSMBuilder operand,
                [Keywords("{")] string brOpen,
                [Many(false)][RequireTags("digit")] string[] start,
                [Keywords(",")] string comma,
                [Many(true)][RequireTags("digit")] string[] end,
                [Keywords("}")] string brClose)
            {
                int x = int.Parse(start.SelectMany(e => e).ToArray());
                if (end.Length == 0)
                    return SmartFSMBuilder.CreateRay(operand, x);

                int y = int.Parse(end.SelectMany(e => e).ToArray());
                return SmartFSMBuilder.CreateSegment(operand, x, y);
            }

            [SetTag("or-branch-item-inbr")]
            public static SmartFSMBuilder Interval_Inbr(
                [RequireTags("or-branch-item-inbr")] SmartFSMBuilder operand,
                [Keywords("{")] string brOpen,
                [Many(false)][RequireTags("digit")] string[] start,
                [Keywords(",")] string comma,
                [Many(true)][RequireTags("digit")] string[] end,
                [Keywords("}")] string brClose)
            {
                return Interval(operand, brOpen, start, comma, end, brClose);
            }
        }

        private static readonly HashSet<char> specials = new()
        {
            '.', '[', ']', '\\', '(', ')', '*', '+', '?', '{', '}', '|', '^', '$', '-', ':', ','
        };
        private static bool IsNotSpecial(char c) => !specials.Contains(c);

        private readonly ParsingEngine engine = new ParsingEngineBuilder()
            .AddTokenViaMachine("digit", SmartFSMBuilder.CreateBySinglePredicate(char.IsDigit).Create())
            .AddTokenViaMachine("non-spec", SmartFSMBuilder.CreateBySinglePredicate(IsNotSpecial).Create())
            .AddProductions<Expr>()
            .Create("expr");

        private RegexParser()
        { }

        private static RegexParser? _instance = null;
        public static RegexParser Instance => _instance ??= new();

        public IMachine Parse(string regexp, SortedDictionary<string, Predicate<char>> charClasses)
        {
            try
            {
                Expr.charClasses = charClasses;
                return engine.Parse<SmartFSMBuilder>(regexp).Create();
            }
            catch(AnalysisStopException e)
            {
                throw new RegexParsingException("Error parsing regexp", e.Token.Line, e.Token.Column);
            }
        }
    }
}
