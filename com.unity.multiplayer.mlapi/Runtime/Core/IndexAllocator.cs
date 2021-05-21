using UnityEngine;

namespace MLAPI
{
    internal struct IndexAllocatorEntry
    {
        internal int pos;
        internal int length;
        internal bool free;
        internal int next;
        internal int prev;
    }

    internal class IndexAllocator
    {
        private const int k_NotSet = -1;
        private readonly int m_MaxSlot;
        private readonly int m_BufferSize;
        private int m_LastSlot = 0;
        private IndexAllocatorEntry[] m_Slots;
        private int[] m_IndexToSlot;

        internal IndexAllocator(int bufferSize, int maxSlot)
        {
            m_MaxSlot = maxSlot;
            m_BufferSize = bufferSize;
            m_Slots = new IndexAllocatorEntry[m_MaxSlot];
            m_IndexToSlot = new int[m_MaxSlot];
            Reset();
        }

        internal void Reset()
        {
            // todo: could be made faster, for example by having a last index
            // and not needing valid stuff past it
            for (int i = 0; i < m_MaxSlot; i++)
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
            m_Slots[m_MaxSlot - 1].next = k_NotSet;
        }


        internal int Range
        {
            get
            {
                // when the whole buffer is free, m_LastSlot points to an empty slot
                if (m_Slots[m_LastSlot].free)
                {
                    return 0;
                }
                // otherwise return the end of the last slot used
                return m_Slots[m_LastSlot].pos + m_Slots[m_LastSlot].length;
            }
        }

        internal bool Allocate(int index, int size, out int pos)
        {
            pos = 0;
            if (m_IndexToSlot[index] != k_NotSet)
            {
                return false;
            }

            // todo: this is the slowest part
            // improvement 1: list of free blocks (minor)
            // improvement 2: heap of free blocks
            for (int i = 0; i < m_MaxSlot; i++)
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

                    pos = m_Slots[i].pos;

                    // if we allocate past the current range, we are the last slot
                    if (m_Slots[i].pos + m_Slots[i].length > Range)
                    {
                        m_LastSlot = i;
                    }

                    break;
                }
            }

            return true;
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

                // if the slot we're merging was the last one, the last one is now the one we merged with
                if (slot == m_LastSlot)
                {
                    m_LastSlot = prev;
                }

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

            // if we just deallocate the last one, we need to move last back
            if (slot == m_LastSlot)
            {
                m_LastSlot = m_Slots[m_LastSlot].prev;
                // if there's nothing allocated anymore, use 0
                if (m_LastSlot == k_NotSet)
                {
                    m_LastSlot = 0;
                }
            }

            // mark the index as available
            m_IndexToSlot[index] = k_NotSet;

            return true;
        }

        // Take a slot at the end and link it to go just after "slot". Used when allocating part of a slot and we need an entry for the rest
        // Returns the slot that was picked
        private int MoveSlotAfter(int slot)
        {
            int ret = m_Slots[m_MaxSlot - 1].prev;
            int p0 = m_Slots[ret].prev;

            m_Slots[p0].next = m_MaxSlot - 1;
            m_Slots[m_MaxSlot - 1].prev = p0;

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

            int p0 = m_Slots[m_MaxSlot - 1].prev;

            m_Slots[p0].next = slot;
            m_Slots[slot].next = m_MaxSlot - 1;

            m_Slots[m_MaxSlot - 1].prev = slot;
            m_Slots[slot].prev = p0;

            m_Slots[slot].pos = m_BufferSize;
        }

        internal bool Verify()
        {
            int pos = k_NotSet;
            int count = 0;
            int total = 0;
            int endPos = 0;

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

                if (!m_Slots[pos].free)
                {
                    endPos = m_Slots[pos].pos + m_Slots[pos].length;
                }

                total += m_Slots[pos].length;
                count++;

            } while (pos != k_NotSet);

            if (count != m_MaxSlot)
            {
                // some slots were lost
                return false;
            }

            if (total != m_BufferSize)
            {
                return false;
            }

            if (endPos != Range)
            {
                Debug.Log(string.Format("{0} range versue {1} end position", Range, endPos));
                return false;
            }

            return true;
        }

        internal void DebugDisplay()
        {
            string logMessage = "IndexAllocator structure\n";

            bool[] seen = new bool[m_MaxSlot];


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
