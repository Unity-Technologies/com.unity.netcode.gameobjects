using System;

namespace MLAPI.Collections
{
    /// <summary>
    /// Queue with a fixed size
    /// </summary>
    /// <typeparam name="T">The type of the queue</typeparam>
    public sealed class FixedQueue<T>
    {
        private readonly T[] queue;
        private int queueCount = 0;
        private int queueStart;

        /// <summary>
        /// The amount of enqueued objects
        /// </summary>
        public int Count { get => queueCount; }

        /// <summary>
        /// Gets the element at a given virtual index
        /// </summary>
        /// <param name="index">The virtual index to get the item from</param>
        /// <returns>The element at the virtual index</returns>
        public T this[int index]
        {
            get
            {
                return queue[(queueStart + index) % queue.Length];
            }
        }

        /// <summary>
        /// Creates a new FixedQueue with a given size
        /// </summary>
        /// <param name="maxSize">The size of the queue</param>
        public FixedQueue(int maxSize)
        {
            queue = new T[maxSize];
            queueStart = 0;
        }

        /// <summary>
        /// Enqueues an object
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Dequeues an object
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            if (--queueCount == -1) throw new IndexOutOfRangeException("Cannot dequeue empty queue!");
            T res = queue[queueStart];
            queueStart = (queueStart + 1) % queue.Length;
            return res;
        }

        /// <summary>
        /// Gets the element at a given virtual index
        /// </summary>
        /// <param name="index">The virtual index to get the item from</param>
        /// <returns>The element at the virtual index</returns>
        public T ElementAt(int index) => queue[(queueStart + index) % queue.Length];
    }
}
