using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;
using CompileLib.EmbeddedLanguage;
using TestCompiler.CodeObjects;

namespace TestCompiler
{
    internal struct CompilationParameters
    {
        public SemanticNetwork SemanticNetwork { get; private set; }
        public ELCompiler Compiler { get; private set; }
        public CodeObject Scope { get; private set; }
        public SortedDictionary<string, Class> Name2Class { get; private set; }
        public ELLabel ContinueLabel { get; private set; }
        public ELLabel BreakLabel { get; private set; }
        public bool HasLabels { get; private set; }

        public CompilationParameters(SemanticNetwork semanticNetwork, ELCompiler compiler, CodeObject scope, SortedDictionary<string, Class> name2Class, ELLabel continueLabel, ELLabel breakLabel, bool hasLabels)
        {
            SemanticNetwork = semanticNetwork;
            Compiler = compiler;
            Scope = scope;
            Name2Class = name2Class;
            ContinueLabel = continueLabel;
            BreakLabel = breakLabel;
            HasLabels = hasLabels;
        }

        public CompilationParameters(SemanticNetwork semanticNetwork, ELCompiler compiler, CodeObject scope, SortedDictionary<string, Class> name2Class)
            : this(semanticNetwork, compiler, scope, name2Class, new(), new(), false)
        {

        }

        public CompilationParameters WithScope(CodeObject scope)
            => new(SemanticNetwork, Compiler, scope, Name2Class);

        public CompilationParameters WithLoop(CodeObject scope, ELLabel continueLabel, ELLabel breakLabel)
            => new(SemanticNetwork, Compiler, scope, Name2Class, continueLabel, breakLabel, true);
    }
}
