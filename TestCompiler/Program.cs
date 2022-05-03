using CompileLib.Parsing;

using CompileLib.EmbeddedLanguage;

ParsingEngine engine;

try
{
    engine = new ParsingEngineBuilder()
        .AddToken("id", @"[[:alpha:]_][[:alnum:]_]*")
        .AddToken("string-const", @"""(\\.|[^\\""[[:cntrl:]]])*""")
        .AddToken(SpecialTags.TAG_SKIP, "[[:space:]]")
        .AddProductions<TestCompiler.Syntax>()
        .Create("program");
}
catch(Exception ex)
{
    Console.WriteLine(ex);
    return;
}

try
{
    Console.WriteLine(engine.ParseFile<object>("test.txt"));
}
catch(Exception ex)
{
    Console.WriteLine(ex);
}

