using TestCompiler;
using CompileLib.Parsing;

ParsingEngine engine;

try
{
    engine = new ParsingEngineBuilder()
        .AddToken("id", @"[[:alpha:]_][[:alnum:]_]*")
        .AddToken("string-const", @"""(\\.|[^\\""[:cntrl:]])*""")
        .AddToken(SpecialTags.TAG_SKIP, "[[:space:]]")
        .AddProductions<Syntax>()
        .Create("program");
}
catch(Exception ex)
{
    Console.WriteLine(ex);
    return;
}

try
{
    var program = engine.ParseFile<Syntax.Program>("test.txt");
    if(program is null)
    {
        Console.WriteLine("Compilation falied");
        return;
    }
    

    program.Compile("result.exe");
    // TODO: run compiler.Build() method
}
catch(Exception ex)
{
    Console.WriteLine(ex);
}

