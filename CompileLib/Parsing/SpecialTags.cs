using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Tags reserved for specific situations
    /// </summary>
    public static class SpecialTags
    {
        /// <summary>
        /// [Only as parameter "tag" in AddRegex method] Tokens tagged with this are skipped in syntax analysis
        /// </summary>
        public const string TAG_SKIP = "_";
        /// <summary>
        /// [Only as result of lexical analysis] Tokens tagged with this are undefined tokens
        /// </summary>
        public const string TAG_UNDEFINED = "?";
        /// <summary>
        /// [Only as argument for Error Handling Decider] The actual tag must be defined
        /// </summary>
        public const string TAG_UNKNOWN = "*";
        /// <summary>
        /// [Only as args in production methods] Tokens tagged with this are keywords
        /// </summary>
        public const string TAG_KEYWORD = "@";
        /// <summary>
        /// [Only as argument for Error Handling Decider] End of file
        /// </summary>
        public const string TAG_EOF = "$";

        private static readonly string[] all =
        {
            TAG_SKIP,
            TAG_UNDEFINED,
            TAG_UNKNOWN,
            TAG_KEYWORD,
            TAG_EOF
        };
        /// <summary>
        /// Determines whether the tag is a special tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static bool IsSpecial(string tag)
        {
            return all.Contains(tag);
        }
    }
}
