using UnityEngine;

namespace MLAPI
{
    internal struct IndexAllocatorEntry
    {
        public int pos;
        public int length;
        public bool free;
        public int next;
        public int prev;
    }

    internal class IndexAllocator
    {
        private const int k_MaxSlot = 128;
        private const int k_NotSet = -1;
        private readonly int m_BufferSize;

        private int FreeMemoryPosition = 0;

        private IndexAllocatorEntry[] m_Slots = new IndexAllocatorEntry[k_MaxSlot];
        private int[] m_IndexToSlot = new int[k_MaxSlot];

        internal IndexAllocator(int bufferSize)
        {
            m_BufferSize = bufferSize;

            for (int i = 0; i < k_MaxSlot; i++)
            {
                m_Slots[i].free = true;
                m_Slots[i].next = i + 1;
                m_Slots[i].prev = i - 1;
                m_Slots[i].pos = m_BufferSize;
                m_Slots[i].length = 0;

                m_IndexToSlot[i] = k_NotSet;
            }

            m_Slots[0].pos = 0;
            m_Slots[0].length = m_BufferSize;
            m_Slots[0].prev = k_NotSet;
            m_Slots[k_MaxSlot - 1].next = k_NotSet;
        }


        public int Range
        {
            get { return FreeMemoryPosition; }
        }

        internal bool Allocate(int index, int size, out int pos)
        {
            if (m_IndexToSlot[index] != k_NotSet)
            {
                pos = 0;
                return false;
            }

            // todo: this is the slowest part
            // improvement 1: list of free blocks (minor)
            // improvement 2: heap of free blocks
            for (int i = 0; i < k_MaxSlot; i++)
            {
                if (m_Slots[i].free && m_Slots[i].length >= size)
                {
                    m_IndexToSlot[index] = i;

                    int leftOver = m_Slots[i].length - size;
                    int next = m_Slots[i].next;
                    if (m_Slots[next].free)
                    {
                        m_Slots[next].pos -= leftOver;
                        m_Slots[next].length += leftOver;
                    }
                    else
                    {
                        int add = MoveSlotAfter(i);

                        m_Slots[add].pos = m_Slots[i].pos + size;
                        m_Slots[add].length = m_Slots[i].length - size;
                    }

                    m_Slots[i].free = false;
                    m_Slots[i].length = size;

                    break;
                }
            }

            pos = FreeMemoryPosition;
            FreeMemoryPosition += (int) size;

            return true;
        }

        // Take a slot at the end and link it to go just after "slot". Used when allocating part of a slot and we need an entry for the rest
        // Returns the slot that was picked
        private int MoveSlotAfter(int slot)
        {
            int ret = m_Slots[k_MaxSlot - 1].prev;
            int p0 = m_Slots[ret].prev;

            m_Slots[p0].next = k_MaxSlot - 1;
            m_Slots[k_MaxSlot - 1].prev = p0;

            int p1 = m_Slots[slot].next;
            m_Slots[slot].next = ret;
            m_Slots[p1].prev = ret;

            m_Slots[ret].prev = slot;
            m_Slots[ret].next = p1;

            return ret;
        }

        // Move the slot "slot" to the end of the list. Used when merging two slots, that gives us an extra entry at the end
        private void MoveSlotToEnd(int slot)
        {
            // if we're already there
            if (m_Slots[slot].next == k_NotSet)
            {
                return;
            }

            int prev = m_Slots[slot].prev;
            int next = m_Slots[slot].next;

            m_Slots[prev].next = next;
            if (next != k_NotSet)
            {
                m_Slots[next].prev = prev;
            }

            int p0 = m_Slots[k_MaxSlot - 1].prev;

            m_Slots[p0].next = slot;
            m_Slots[slot].next = k_MaxSlot - 1;

            m_Slots[k_MaxSlot - 1].prev = slot;
            m_Slots[slot].prev = p0;

            m_Slots[slot].pos = m_BufferSize;
        }

        internal bool Deallocate(int index)
        {
            int slot = m_IndexToSlot[index];

            if (slot == k_NotSet)
            {
                return false;
            }

            if (m_Slots[slot].free)
            {
                return false;
            }

            m_Slots[slot].free = true;

            int prev = m_Slots[slot].prev;
            int next = m_Slots[slot].next;

            // if previous slot was free, merge and grow
            if (prev != k_NotSet && m_Slots[prev].free)
            {
                m_Slots[prev].length += m_Slots[slot].length;
                m_Slots[slot].length = 0;

                // todo: verify what this does on full or nearly full cases
                MoveSlotToEnd(slot);
                slot = prev;
            }

            next = m_Slots[slot].next;

            // merge with next slot if it is free
            if (next != k_NotSet && m_Slots[next].free)
            {
                m_Slots[slot].length += m_Slots[next].length;
                m_Slots[next].length = 0;
                MoveSlotToEnd(next);
            }

            // mark the index as available
            m_IndexToSlot[index] = k_NotSet;

            return true;
        }

        internal bool Verify()
        {
            int pos = k_NotSet;
            int count = 0;
            int total = 0;

            do
            {
                int prev = pos;
                if (pos != k_NotSet)
                {
                    pos = m_Slots[pos].next;
                    if (pos == k_NotSet)
                    {
                        break;
                    }
                }
                else
                {
                    pos = 0;
                }

                if (m_Slots[pos].prev != prev)
                {
                    // the previous is not correct
                    return false;
                }

                if (m_Slots[pos].length < 0)
                {
                    // length should be positive
                    return false;
                }

                if (prev != k_NotSet && m_Slots[prev].free && m_Slots[pos].free && m_Slots[pos].length > 0)
                {
                    // should not have two consecutive free slots
                    return false;
                }

                if (m_Slots[pos].pos != total)
                {
                    // slots should all line up nicely
                    return false;
                }

                total += m_Slots[pos].length;
                count++;

            } while (pos != k_NotSet);

            if (count != k_MaxSlot)
            {
                // some slots were lost
                return false;
            }

            if (total != m_BufferSize)
            {
                return false;
            }

            return true;
        }

        internal void DebugDisplay()
        {
            string logMessage = "IndexAllocator structure\n";

            bool[] seen = new bool[k_MaxSlot];


            int pos = 0;
            int count = 0;
            bool prevEmpty = false;
            do
            {
                seen[pos] = true;
                count++;
                if (m_Slots[pos].length == 0 && prevEmpty)
                {

                }
                else
                {
                    logMessage += string.Format("{0}:{1}, {2} ({3}) \n", m_Slots[pos].pos, m_Slots[pos].length,
                        m_Slots[pos].free ? "Free" : "Used", pos);
                    if (m_Slots[pos].length == 0)
                    {
                        prevEmpty = true;
                    }
                    else
                    {
                        prevEmpty = false;
                    }
                }

                pos = m_Slots[pos].next;
            } while (pos != k_NotSet && !seen[pos]);

            logMessage += string.Format("{0} Total entries\n", count);

            Debug.Log(logMessage);
        }
    }
}
