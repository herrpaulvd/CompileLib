using TestCompiler;
using TestCompiler.CodeObjects;
using CompileLib.Parsing;

//
ParsingEngine engine;

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
        .AddProductions<Syntax>()
        .Create("global");
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
    return;
}

string PromptInput(string prompt)
{
    Console.Write(prompt);
    return Console.ReadLine();
}

try
{
    var global = engine.ParseFile<GlobalScope>(args.Length > 0 ? args[0] : PromptInput("Input file: "));
    global?.Compile(args.Length > 1 ? args[1] : PromptInput("Output file: "));
}
catch(ArgumentNullException ex)
{
    Console.WriteLine(ex.Message);
    return;
}

