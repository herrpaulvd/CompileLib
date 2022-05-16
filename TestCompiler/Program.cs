using TestCompiler;
using TestCompiler.CodeObjects;
using CompileLib.Parsing;

Console.WriteLine(Resources.searchrules);

ParsingEngine engine;

// (TODO???: op-priorities)
// TODO: compilation using search rules!!!
try
{
    engine = new ParsingEngineBuilder()
        .AddToken("id", @"[[:alpha:]_][[:alnum:]_]*")
        .AddToken("str", @"""(\\.|[^\\""[:cntrl:]])*""")
        .AddToken("char", @"'(\\.|[^\\'[:cntrl:]])'")
        .AddToken("int10", @"[1-9][0-9]*|0")
        .AddToken("int16", @"0x[[:xdigit:]]+")
        .AddToken("int8", @"0[0-7]+")
        .AddToken("int2", @"0b[01]+")
        .AddToken(SpecialTags.TAG_SKIP, "[[:space:]]")
        .AddToken(SpecialTags.TAG_SKIP, @"//[^[:cntrl:]]*")
        .AddProductions<OldSyntax>()
        .Create("program");
}
catch(Exception ex)
{
    Console.WriteLine(ex);
    return;
}

try
{
    var global = engine.ParseFile<OldSyntax.Program>("test.txt");
    global?.Compile("result.exe");
}
catch(Exception ex)
{
    Console.WriteLine(ex);
    return;
}

