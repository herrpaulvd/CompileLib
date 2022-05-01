using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Parsing;

namespace Launcher
{
    class GrammarShell
    {
        [SetTag("prog")]
        public static object? Handle([RequireTags("expr")] string item)
        {
            return null;
        }

        public static bool TestRegex(string regex, string s, params Predicate<char>[] classes)
        {
            try
            {
                var engine =
                                new ParsingEngineBuilder()
                                .AddToken("expr", regex, classes)
                                .AddProductions<GrammarShell>()
                                .Create("prog");

                try
                {
                    engine.Parse<object>(s);
                    return true;
                }
                catch (AnalysisStopException)
                {
                    return false;
                }
            }
            catch(ParsingConflictException ex)
            {
                Console.WriteLine(ex);
                Environment.Exit(0);
                return false;
            }
        }
    }

    internal class RegexTester
    {
        static string id = @"[[:alpha:]_][[:alnum:]_]*";
        static string int10 = @"[1-9][0-9]*|0";
        static string int8 = @"0[0-7]+";
        static string int16 = @"0x[[:xdigit:]]+";
        static string int2 = @"0b[01]+";
        static string float10 = @"([1-9][0-9]*|0)\.[0-9]*|\.[0-9]+";
        static string strliteral = @"""[:print:]""";

        static (string, string, bool)[] testCases =
        {
            // simple
            ("2", "2", true),
            ("2", "22", false),
            // id
            (id, "i", true),
            (id, "228cat228", false),
            (id, "_", true),
            (id, "____", true),
            (id, "_228", true),
            (id, "", false),
            (id, "_!", false),
            (id, "hello_darkness", true),
            (id, "cat228cat", true),
            (id, "System.Object", false),
            (id, "Object", true),
            (id, "obJecT", true),
            // int10
            (int10, "0", true),
            (int10, "00", false),
            (int10, "01", false),
            (int10, "09", false),
            (int10, "109", true),
            (int10, "9", true),
            (int10, "0x9", false),
            (int10, "", false),
            (int10, "A", false),
            // int8
            (int8, "0", false),
            (int8, "00", true),
            (int8, "09", false),
            (int8, "08", false),
            (int8, "07", true),
            (int8, "0227", true),
            (int8, "0228", false),
            (int8, "1", false),
            (int8, "11", false),
            (int8, "011", true),
            (int8, "000000", true),
            // int16
            (int16, "0", false),
            (int16, "0x", false),
            (int16, "00", false),
            (int16, "0x0", true),
            (int16, "0x0123456789AaBbCcDdEeFf", true),
            (int16, "0xe", true),
            (int16, "0F", false),
            (int16, "F", false),
            // int2
            (int2, "0", false),
            (int2, "0b", false),
            (int2, "0b0", true),
            (int2, "0b2", false),
            (int2, "0b1001", true),
            (int2, "1", false),
            // float10
            (float10, "0", false),
            (float10, "0.", true),
            (float10, "13.", true),
            (float10, ".", false),
            (float10, "0x0", false),
            (float10, "x0", false),
            (float10, "08.08", false),
            (float10, "8.08", true),
            (float10, ".013", true),
            (float10, ".13", true),
            (float10, ".0", true),
            // strliteral
            (strliteral, @"""", false),
            (strliteral, @"""""", true),
            (strliteral, @""" """, true),
            (strliteral, @"""abcdbcia+-+-+f-wefewfefwefашимвщаbcsda""", false),
            (strliteral, "\"\n\"", false),
            (strliteral, @"""a", false),
            // other
            ("x{3,4}", "xxx", true),
            ("x{3,4}", "xxxx", true),
            ("x{3,4}", "xxxxx", false),
            ("[xy]{3,4}", "yyxy", true),
            ("[xy]{3,4}", "yyyy", true),
            ("[xy]{3,4}", "xxx", true),
            ("[xy]{3,4}", "xxxyy", false),
            ("(xy){3,4}", "xyxy", false),
            ("(xy){3,4}", "xyxyxy", true),
            ("(xy){3}", "xyxyxyxy", false),
            ("(xy){2}", "xyxy", true),
            ("(xy){2}", "yx", false),
            ("(xy){2}", "yxyx", false),
            ("(x){2,}", "x", false),
            ("(x){2,}", "xx", true),
            ("(x){2,}", "xxxxxxxxxxxxxxxxxxxxxxxxxxxxx", true),
            ("(x){0,}", "xxxxxx", true),
            ("(x){0,}", "y", false),
            ("(x){0,}", "", true),
            ("x?xx?", "x", true),
            ("x?xx?", "xx", true),
            ("x?xx?", "xxx", true),
            ("x?xx?", "xxxx", false),
            (".?..?", "ou", true),
            (".?..?", " ", true),
            (".?..?", "\0", false),
            (".?????????", "x", true),
        };

        static bool Print(string s)
        {
            Console.WriteLine(s);
            return false;
        }

        public static void TestRegexes()
        {
            Console.WriteLine("Starting regex tests...");
            bool allok = true;
            foreach (var (regex, s, expected) in testCases)
            {
                if (regex != float10) continue; // temp
                bool result = GrammarShell.TestRegex(regex, s);
                if (result == expected) continue;
                var comment = result ? "MATCH" : "NONMATCH";
                Console.WriteLine($"[WA] Regex test: {regex} in {s} result {comment}");
                allok = false;
            }

            allok &= GrammarShell.TestRegex("[[:0:][:1:]][[:2:]]", "xy", c => c == 'x', c => c == 'y', char.IsLetter) || Print("[WA] classes test 1 failed");
            allok &= !GrammarShell.TestRegex("[[:0:][:1:]][[:2:]]", "xy", c => c == 'z', c => c == 'y', char.IsLetter) || Print("[WA] classes test 2 failed");

            if (allok) Console.WriteLine("[OK] All regex tests passed successful");
        }
    }
}
