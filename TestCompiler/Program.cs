using TestCompiler;
using CompileLib.Parsing;

Console.WriteLine(Resources.searchrules);

ParsingEngine engine;

// TODO???: op-priorities
// TODO: write the whole syntax
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
    var program = engine.ParseFile<object>("test.txt");
    if(program is null)
    {
        Console.WriteLine("Compilation falied");
        return;
    }

    //program.Compile("result.exe");
}
catch(Exception ex)
{
    Console.WriteLine(ex);
    return;
}

