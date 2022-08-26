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

        public Lexer((int, IMachine)[] tokens)
        {
            this.tokens = tokens;
        }

        private static void ChangePosition(char newChar, ref int line, ref int column, ref char oldChar)
        {
            if(oldChar == '\r' && newChar == '\n')
            {
                oldChar = newChar;
                return;
            }

            if(newChar == '\r' || newChar == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }

            oldChar = newChar;
            return;
        }

        /// <summary>
        /// id == null means no valid token
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public IEnumerable<Token> GetTokens(IEnumerable<char> code)
        {
            var s = new BufferedEnumerator<char>(code);

            int line = 1;
            int column = 1;
            char oldChar = 'a';
            
            while (s.MoveNext())
            {
                int? tokenID = null;
                int tokenLength = 0;
                int tokenLine = line;
                int tokenColumn = column;

                int newLine = line;
                int newColumn = column;
                char newLastChar = oldChar;

                s.Restart();
                var q = new Queue<(int, IMachine)>(tokens);
                foreach(var (_, machine) in tokens)
                    machine.Start();

                for(int length = 1; q.Count > 0 && s.MoveNext(); length++)
                {
                    ChangePosition(s.Current, ref line, ref column, ref oldChar);

                    int machineCount = q.Count;
                    for(int i = 0; i < machineCount; i++)
                    {
                        var (id, machine) = q.Dequeue();
                        if (machine.Tact(s.Current))
                            q.Enqueue((id, machine));
                        if(length > tokenLength && machine.StateIsFinal)
                        {
                            tokenID = id;
                            tokenLength = length;
                            newLine = line;
                            newColumn = column;
                            newLastChar = oldChar;
                        }
                    }
                }

                if (tokenLength == 0)
                {
                    var tk = s.Extract(1);
                    yield return new Token(tokenID, new string(tk), tokenLine, tokenColumn);
                    line = newLine;
                    column = newColumn;
                    oldChar = newLastChar;
                    ChangePosition(tk[0], ref line, ref column, ref oldChar);
                }
                else
                {
                    yield return new Token(tokenID, new string(s.Extract(tokenLength)), tokenLine, tokenColumn);
                    line = newLine;
                    column = newColumn;
                    oldChar = newLastChar;
                }
            }
        }

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
