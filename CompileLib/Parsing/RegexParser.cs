using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using CompileLib.LexerTools;

namespace CompileLib.Parsing
{
    internal class RegexParser
    {
        private class Expr
        {
            // whole expression
            [SetTag("expr")]
            public static SmartFSMBuilder MakeExpression(
                [Optional][RequireTags("expr")] SmartFSMBuilder expr,
                [TogetherWith][Keywords("|")] string _,
                [RequireTags("or-branch")] SmartFSMBuilder branch
                )
            {
                if (expr is null)
                    return branch;
                else
                    return SmartFSMBuilder.CreateUnion(expr, branch);
            }

            [SetTag("or-branch")]
            public static SmartFSMBuilder OrBranch(
                [Many(false)][RequireTags("or-branch-item")] SmartFSMBuilder[] items)
            {
                return items.Aggregate(SmartFSMBuilder.CreateConcatenation);
            }

            [SetTag("or-branch-item")]
            public static SmartFSMBuilder OrBranchItem_nonSpec(
                [RequireTags("non-spec", "digit")] string s)
            {
                char it = s[0];
                return SmartFSMBuilder.CreateBySinglePredicate(c => c == it);
            }

            [SetTag("or-branch-item")]
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
                [Keywords(".", "]", "}", "^", "$", "-", ":", ",")] string s)
            {
                char it = s[0];
                if (it == '.')
                    return SmartFSMBuilder.CreateBySinglePredicate(c => c != 0);
                else
                    return SmartFSMBuilder.CreateBySinglePredicate(c => c == it);
            }

            [SetTag("or-branch-item")]
            [ProductionPriority(-1)]
            public static SmartFSMBuilder OrBranchSpecChar_brClose([Keywords(")")] string _)
                => SmartFSMBuilder.CreateBySinglePredicate(c => c == ')');

            [SetTag("or-branch-item")]
            public static SmartFSMBuilder BracketExpr(
                [Keywords("[")] string brOpen,
                [Optional][Keywords("^")] string circumFlex,
                [Many(false)][RequireTags("bracket-expr-item")] Predicate<char>[] items,
                [Keywords("]")] string brClose
                )
            {
                return items.Select(
                    circumFlex is null
                    ? new Func<Predicate<char>, SmartFSMBuilder>(SmartFSMBuilder.CreateBySinglePredicate)
                    : new Func<Predicate<char>, SmartFSMBuilder>(p => SmartFSMBuilder.CreateBySinglePredicate(c => !p(c))))
                    .Aggregate(SmartFSMBuilder.CreateUnion);
            }

            [SetTag("bracket-expr-item-simple")]
            public static string BracketExprItem(
                [RequireTags("non-spec", "digit")] string operand)
            {
                return operand;
            }

            [SetTag("bracket-expr-item-simple")]
            public static string BracketExprSpec(
                [Keywords(".", "[", "\\", "(", ")", "*", "+", "?", "{", "}", "|", "$", ":", ",")] string s)
            {
                return s;
            }

            [SetTag("bracket-expr-item-simple")]
            [ProductionPriority(-1)]
            public static string BracketExprSpec2([Keywords("]", "^", "-")] string s)
            {
                return s;
            }

            [SetTag("bracket-expr-item")]
            public static Predicate<char> BracketExpr_simpleItem([RequireTags("bracket-expr-item-simple")] string s)
            {
                char it = s[0];
                return c => c == it;
            }

            internal static IDictionary<string, Predicate<char>> charClasses;

            [SetTag("bracket-expr-item")]
            [ProductionPriority(1)]
            public static Predicate<char> CharClass(
                [Keywords("[")] string sqBrOpen,
                [Keywords(":")] string colon1,
                [Many(false)][RequireTags("non-spec", "digit")] Token[] name,
                [Keywords(":")] string colon2,
                [Keywords("]")] string sqBrClose)
            {
                string s = new(name.SelectMany(e => e.Self).ToArray());
                if (charClasses.ContainsKey(s))
                    return charClasses[s];
                else
                    throw new RegexParsingException("Invalid char class", name[0].Line, name[0].Column);
            }

            [SetTag("bracket-expr-item")]
            [ProductionPriority(1)]
            public static Predicate<char> Range(
                [RequireTags("bracket-expr-item-simple")] string start,
                [Keywords("-")] string _,
                [RequireTags("bracket-expr-item-simple")] string end
                )
            {
                char startChar = start[0];
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

            [SetTag("or-branch-item")]
            public static SmartFSMBuilder Interval(
                [RequireTags("or-branch-item")] SmartFSMBuilder operand,
                [Keywords("{")] string brOpen,
                [Many(false)][RequireTags("digit")] string[] count,
                [Keywords("}")] string brClose)
            {
                return SmartFSMBuilder.CreateSimpleDup(operand, int.Parse(count.SelectMany(e => e).ToArray()));
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
