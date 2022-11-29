using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Common
{
    /// <summary>
    /// Char enumerator with some useful options
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class BufferedCharEnumerator
    {
        private readonly LinkedList<CodeChar> buffer = new();
        private readonly IEnumerator<char> enumerator;
        private readonly LinkedListNode<CodeChar> start = new(new());
        private LinkedListNode<CodeChar>? current;
        private bool bufferOnly = false;
        private int line = 1;
        private int column = 1;
        private char oldChar = 'a';
        private Func<char, bool>? stopCondition = null;

        public BufferedCharEnumerator(IEnumerable<char> stream)
        {
            enumerator = stream.GetEnumerator();
            buffer.AddFirst(start);
            current = start;
        }

        private void ChangePosition(char newChar)
        {
            if (oldChar == '\r' && newChar == '\n')
            {
                oldChar = newChar;
                return;
            }

            if (newChar == '\r' || newChar == '\n')
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
        /// Move to the next character
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (current is null)
                return false;

            current = current.Next;
            if (current is null)
            {
                if (bufferOnly)
                    return false;

                if (enumerator.MoveNext())
                {
                    char newChar = enumerator.Current;
                    buffer.AddLast(new LinkedListNode<CodeChar>(new(newChar, line, column)));
                    ChangePosition(newChar);
                    oldChar = newChar;
                    current = buffer.Last;
                }
                else
                {
                    bufferOnly = true;
                    return false;
                }
            }

            return stopCondition is null || !stopCondition(current.Value.c);
        }

        /// <summary>
        /// Adds condition that cancels move next if the next char satisfies the condition
        /// </summary>
        /// <param name="stopCondition"></param>
        public void AddStopCondition(Func<char, bool> stopCondition)
        {
            this.stopCondition = stopCondition;
        }

        /// <summary>
        /// Cancels the stop condition and performs MoveNext operation
        /// </summary>
        /// <returns></returns>
        public bool CancelStopConditionAndMoveNext()
        {
            stopCondition = null;
            return current is not null;
        }

        /// <summary>
        /// Get current character
        /// </summary>
        public CodeChar Current
        {
            get
            {
                Debug.Assert(current is not null);
                return current.Value;
            }
        }

        /// <summary>
        /// Restart reading the character sequence
        /// </summary>
        public void Restart()
        {
            current = start;
        }

        /// <summary>
        /// Restarts, moves to the first character that does not satisfy the condition and extracts all the previous characters
        /// </summary>
        /// <param name="ignorationCondition"></param>
        /// <returns></returns>
        public bool Restart(Func<char, bool> ignorationCondition)
        {
            while (true)
            {
                Restart();
                if (!MoveNext())
                    return false;
                Debug.Assert(start.Next is not null);
                if (ignorationCondition(Current.c))
                    buffer.Remove(start.Next);
                else
                    return true;
            }
        }

        /// <summary>
        /// Removes the given number of character making impossible to restart reading from any of them
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public CodeChar[] Extract(int count)
        {
            var result = new CodeChar[count];
            for (int i = 0; i < count; i++)
            {
                Debug.Assert(start.Next is not null);
                result[i] = start.Next.Value;
                buffer.Remove(start.Next);
            }
            Restart();
            return result;
        }
    }
}
