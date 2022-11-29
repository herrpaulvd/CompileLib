using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompileLib.Common;

namespace CompileLib.LexerTools
{
    internal class Lexer
    {
        private readonly (int, IMachine)[] tokens;
        private bool is2d;
        private int lineEnd, blockBegin, blockEnd;

        public Lexer((int, IMachine)[] tokens)
        {
            this.tokens = tokens;
            is2d = false;
        }

        public Lexer((int, IMachine)[] tokens, int lineEnd, int blockBegin, int blockEnd)
        {
            this.tokens = tokens;
            is2d = true;
            this.lineEnd = lineEnd;
            this.blockBegin = blockBegin;
            this.blockEnd = blockEnd;
        }

        /// <summary>
        /// Extracts tokens from the given char stream
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private IEnumerable<Token> GetTokensFromSegment(BufferedCharEnumerator s)
        {
            while (s.MoveNext())
            {
                int? tokenID = null;
                int tokenLength = 0;

                s.Restart();
                var q = new Queue<(int, IMachine)>(tokens);
                foreach(var (_, machine) in tokens)
                    machine.Start();

                for(int length = 1; q.Count > 0 && s.MoveNext(); length++)
                {
                    int machineCount = q.Count;
                    for(int i = 0; i < machineCount; i++)
                    {
                        var (id, machine) = q.Dequeue();
                        if (machine.Tact(s.Current.c))
                            q.Enqueue((id, machine));
                        if(length > tokenLength && machine.StateIsFinal)
                        {
                            tokenID = id;
                            tokenLength = length;
                        }
                    }
                }

                yield return s.Extract(Math.Max(1, tokenLength)).ToToken(tokenID);
            }
        }

        private IEnumerable<Token> GetTokens2D(IEnumerable<char> code)
        {
            Stack<int> stack = new();
            BufferedCharEnumerator s = new(code);
            Token lastToken = new(-1, "", 1, 1);
            while(s.Restart(char.IsWhiteSpace))
            {
                int line = s.Current.line;
                int column = s.Current.column;
                while (stack.Count > 0 && stack.Peek() > column)
                {
                    stack.Pop();
                    if(stack.Count > 0)
                        yield return new(blockEnd, "", line, column);
                }

                if(stack.Count == 0)
                {
                    stack.Push(column);
                }
                else if (stack.Peek() < column)
                {
                    yield return new(blockBegin, "", line, column);
                    stack.Push(column);
                }

                s.Restart();
                s.AddStopCondition(char.IsControl);
                foreach (var token in GetTokensFromSegment(s))
                    yield return lastToken = token;
                if (s.CancelStopConditionAndMoveNext())
                    yield return new(lineEnd, "", s.Current.line, s.Current.column);
                else
                    yield return new(lineEnd, "", lastToken.Line, lastToken.Column + lastToken.Self.Length);
            }
            for (int i = 1; i < stack.Count; i++)
                yield return new(blockEnd, "", lastToken.Line, lastToken.Column + lastToken.Column + lastToken.Self.Length);
        }

        public IEnumerable<Token> GetTokens(IEnumerable<char> code)
            => is2d ? GetTokens2D(code) : GetTokensFromSegment(new(code)); 

        public int? SingleAnalyze(string token)
        {
            foreach(var (id, machine) in tokens)
            {
                bool fullRead = true;
                machine.Start();
                foreach (var c in token)
                    if (!machine.Tact(c))
                    {
                        fullRead = false;
                        break;
                    }

                if (fullRead && machine.StateIsFinal)
                    return id;
            }

            return null;
        }
    }
}
