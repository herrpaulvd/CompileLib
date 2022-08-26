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
        .AddToken("float", @"([1-9][0-9]*|0)\.[0-9]*|\.[0-9]+")
        .AddToken(SpecialTags.TAG_SKIP, "[[:space:]]")
        .AddToken(SpecialTags.TAG_SKIP, @"//[^[:cntrl:]]*")
        .AddProductions<Syntax>()
        .Create("global");
}
catch(Exception ex)
{
    Console.WriteLine("There is an internal error:");
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
    Syntax.ErrorList.Clear();
    var global = engine.ParseFile<GlobalScope>(args.Length > 0 ? args[0] : PromptInput("Input file: "));
    if(Syntax.ErrorList.Empty())
    {
        global?.Self?.Compile(args.Length > 1 ? args[1] : PromptInput("Output file: "));
    }
    else
    {
        Console.WriteLine("There are errors:");
        Console.WriteLine(Syntax.ErrorList);
    }
}
catch(ArgumentException ex)
{
    // TODO: какая-то ошибка, вместо типа вылазеет [helper tag]#43 проследить
    Console.WriteLine("There are errors:");
    if (!Syntax.ErrorList.Empty()) Console.WriteLine(Syntax.ErrorList);
    Console.WriteLine(ex.Message);
    return;
}

Console.WriteLine("Successful compilation");

