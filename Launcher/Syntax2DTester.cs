using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Parsing;

namespace Launcher
{
    internal class Syntax2DTester
    {
        private class TestObject
        {
            public string Name { get; }
            public object Value { get; } // TestObject[] or int
            
            public bool IsInt => Value is int;
            public int IntValue => (int)Value;
            public TestObject[] Children => (TestObject[])Value;

            public TestObject(string name, object value)
            {
                Name = name;
                Value = value;
            }

            public void Print(int level)
            {
                for (int i = 0; i < level; i++)
                    Console.Write(">");
                Console.Write(Name);
                if(IsInt)
                {
                    Console.Write("~");
                    Console.WriteLine(Value);
                }
                else
                {
                    Console.WriteLine("$");
                    foreach (var c in Children)
                        c.Print(level + 1);
                }
            }
        }

        private class Syntax
        {
            [SetTag("object")]
            public static TestObject ReadIntObject(
                [RequireTags("name")] string name,
                [Keywords("=")] string eq,
                [RequireTags("int")] string value,
                [RequireTags("line-end")] string lineEnd
                )
            {
                return new(name, int.Parse(value));
            }

            [SetTag("object")]
            public static TestObject ReadComplexObject(
                [RequireTags("name")] string name,
                [Keywords(":")] string colon,
                [RequireTags("line-end")] string lineEnd,
                [RequireTags("block-begin")] string blockBegin,
                [Many(false)][RequireTags("object")] TestObject[] children,
                [RequireTags("block-end")] string blockEnd
                )
            {
                return new(name, children);
            }
        }

        public static void Test()
        {
            var engine = new ParsingEngineBuilder()
                .Enable2DSyntax("line-end", "block-begin", "block-end")
                .AddToken("name", @"[[:alpha:]_][[:alnum:]_]*")
                .AddToken("int", @"[0-9]*")
                .AddToken(SpecialTags.TAG_SKIP, "[[:space:]]")
                .AddToken(SpecialTags.TAG_SKIP, @"//[^[:cntrl:]]*")
                .AddProductions<Syntax>()
                .Create("object");
            var obj = engine.ParseFile<TestObject>("2din.txt").Self;
            obj.Print(0);
        }
    }
}
