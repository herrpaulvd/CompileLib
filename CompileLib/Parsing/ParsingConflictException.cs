using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Represents one of the rules having the conflict
    /// </summary>
    public struct ParsingConflictOpponent
    {
        /// <summary>
        /// Tag of the expression. Null if it's the whole program
        /// </summary>
        public string? Tag;
        /// <summary>
        /// Right part of the rule of the expression. Null <=> Tag is null.
        /// </summary>
        public string? RuleView;
        /// <summary>
        /// If true, the Token goes after the expression, otherwise it is a part of the expression
        /// </summary>
        public bool IsCarry;

        public ParsingConflictOpponent(string? tag, string? ruleView, bool isCarry)
        {
            Tag = tag;
            RuleView = ruleView;
            IsCarry = isCarry;
        }
    }

    public class ParsingConflictException : ParsingEngineBuildingException
    {
        /// <summary>
        /// Sequence of tags describing the situation where the conflict exists
        /// </summary>
        public string[] Way { get; }
        /// <summary>
        /// The token to be performed after Way OR (if null) the end of file
        /// </summary>
        public string? Token { get; }
        /// <summary>
        /// One of the rules having the conflict
        /// </summary>
        public ParsingConflictOpponent FirstOpponent { get; }
        /// <summary>
        /// One of the rules having the conflict
        /// </summary>
        public ParsingConflictOpponent SecondOpponent { get; }

        private static string CreateMessage(string[] way, string? token, ParsingConflictOpponent first, ParsingConflictOpponent second)
        {
            StringBuilder result = new(
                "The grammar seems to be ambiguous.\n" +
                "Supposing the analyzator has read some expressions represented by the following sequence of tags:\n");

            result.Append(string.Join(' ', way));
            result.Append('\n');

            if (token is null)
            {
                result.Append("The end of file");
            }
            else
            {
                result.Append("The next token represented by the tag ");
                result.Append(token);
            }
            result.Append(" has ambiguous interpretation. ");

            void DescribeOpponent(string start, ParsingConflictOpponent opponent)
            {
                result.Append(start);

                if (token is null)
                    result.Append(" it ends ");
                else if (opponent.IsCarry)
                    result.Append(" it is a part of ");
                else
                    result.Append(" it is a part of another expression going after or including ");
                if(opponent.Tag is null)
                {
                    result.Append("the whole program");
                }
                else
                {
                    result.Append("an expression ");
                    result.Append(opponent.Tag);
                    result.Append(" constructed according to the rule:\n");
                    result.Append(opponent.RuleView);
                }
                result.Append('\n');
            }

            DescribeOpponent("Either", first);
            DescribeOpponent("or", second);
            return result.ToString();
        }

        public ParsingConflictException(string[] way, string? token, ParsingConflictOpponent firstOpponent, ParsingConflictOpponent secondOpponent)
            : base(CreateMessage(way, token, firstOpponent, secondOpponent))
        {
            Way = way;
            Token = token;
            FirstOpponent = firstOpponent;
            SecondOpponent = secondOpponent;
        }
    }
}
