using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CompileLib.Common
{
    /// <summary>
    /// Enumerator with restarting
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class BufferedEnumerator<T> where T : new()
    {
        private readonly LinkedList<T> buffer = new();
        private readonly IEnumerator<T> enumerator;
        private readonly LinkedListNode<T> start = new(new());
        private LinkedListNode<T>? current;
        private bool bufferOnly = false;

        public BufferedEnumerator(IEnumerable<T> stream)
        {
            enumerator = stream.GetEnumerator();
            buffer.AddFirst(start);
            current = start;
        }

        /// <summary>
        /// Move to next character
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (current is null)
                return false;

            current = current.Next;
            if(current is null)
            {
                if(bufferOnly)
                    return false;

                if(enumerator.MoveNext())
                {
                    buffer.AddLast(new LinkedListNode<T>(enumerator.Current));
                    current = buffer.Last;
                    return true;
                }
                else
                {
                    bufferOnly = true;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get current character
        /// </summary>
        public T Current
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
        /// Remove the given number of character making impossible to restart reading from any of them
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public T[] Extract(int count)
        {
            var result = new T[count];
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
