using System;

namespace MLAPI.Data
{
    public class FixedQueue<T>
    {
        protected readonly T[] queue;
        protected int queueCount = 0;
        protected int queueStart;

        public int Count { get => queueCount; }

        public FixedQueue(int maxSize)
        {
            queue = new T[maxSize];
            queueStart = 0;
        }

        public bool Enqueue(T t)
        {
            queue[(queueStart + queueCount) % queue.Length] = t;
            if (++queueCount > queue.Length)
            {
                --queueCount;
                return true;
            }
            return false;
        }

        public T Dequeue()
        {
            if (--queueCount == -1) throw new IndexOutOfRangeException("Cannot dequeue empty queue!");
            T res = queue[queueStart];
            queueStart = (queueStart + 1) % queue.Length;
            return res;
        }

        public T ElementAt(int index) => queue[(queueStart + index) % queue.Length];
    }
}
