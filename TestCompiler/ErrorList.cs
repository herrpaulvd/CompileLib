using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Parsing;

namespace TestCompiler
{
    internal class ErrorList
    {
        private List<(string, int, int)> errors = new();

        public void Add(string message, int line, int column)
            => errors.Add((message, line, column));

        public void AddExpectation(string what, int line, int column)
            => Add($"{what} expected", line, column);

        public void AddUnexpected(string what, int line, int column)
            => Add($"Unexpected {what}", line, column);

        public void AddUnexpected(Parsed<string> tk)
            => AddUnexpected(tk.Tag == SpecialTags.TAG_KEYWORD ? $"Keyword '{tk.Self}'" : tk.Tag, tk.Line, tk.Column);

        public bool Empty() => errors.Count == 0;

        public override string ToString()
        {
            StringBuilder result = new();
            foreach (var (message, line, column) in errors)
                result.Append($"{message} at {line}:{column}\n");
            return result.ToString();
        }

        public void Clear() => errors.Clear();
    }
}
