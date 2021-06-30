using System;

namespace MLAPI.Collections
{
    /// <summary>
    /// Queue with a fixed size
    /// </summary>
    /// <typeparam name="T">The type of the queue</typeparam>
    public sealed class FixedQueue<T>
    {
        private readonly T[] m_Queue;
        private int m_QueueCount = 0;
        private int m_QueueStart;

        /// <summary>
        /// The amount of enqueued objects
        /// </summary>
        public int Count => m_QueueCount;

        /// <summary>
        /// Gets the element at a given virtual index
        /// </summary>
        /// <param name="index">The virtual index to get the item from</param>
        /// <returns>The element at the virtual index</returns>
        public T this[int index] => m_Queue[(m_QueueStart + index) % m_Queue.Length];

        /// <summary>
        /// Creates a new FixedQueue with a given size
        /// </summary>
        /// <param name="maxSize">The size of the queue</param>
        public FixedQueue(int maxSize)
        {
            m_Queue = new T[maxSize];
            m_QueueStart = 0;
        }

        /// <summary>
        /// Enqueues an object
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool Enqueue(T t)
        {
            m_Queue[(m_QueueStart + m_QueueCount) % m_Queue.Length] = t;
            if (++m_QueueCount > m_Queue.Length)
            {
                --m_QueueCount;
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
            if (--m_QueueCount == -1) throw new IndexOutOfRangeException("Cannot dequeue empty queue!");
            T res = m_Queue[m_QueueStart];
            m_QueueStart = (m_QueueStart + 1) % m_Queue.Length;
            return res;
        }

        /// <summary>
        /// Gets the element at a given virtual index
        /// </summary>
        /// <param name="index">The virtual index to get the item from</param>
        /// <returns>The element at the virtual index</returns>
        public T ElementAt(int index) => m_Queue[(m_QueueStart + index) % m_Queue.Length];
    }
}