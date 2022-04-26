using CompileLib.Parsing;
using CompileLib.LexerTools;

using System.Reflection;
using System.Text;

namespace CompileLib
{

    internal class SimpleG
    {
        [SetTag("s")]
        public static string Print([RequireTags("q")] string tk, [ErrorHandler] ErrorHandlingDecider ehd)
        {
            if (ehd is null)
            {
                return tk;
            }
            else
            {
                return "<СУКААА>";
            }
        }
    }

    public class TestClass
    {
        public static void Main(string[] args)
        {
            try
            {
                var engine =
                    new ParsingEngineBuilder()
                    .AddToken("q", "[a-z]+")
                    .AddProductions<SimpleG>()
                    .Create("s");

                Console.Write("File: ");
                Console.WriteLine("Result:\n" + engine.ParseFile<string>(@"C:\Users\herrp\Desktop\test.txt"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
