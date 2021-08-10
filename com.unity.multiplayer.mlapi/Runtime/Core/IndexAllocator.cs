using UnityEngine;

namespace Unity.Netcode
{
    internal struct IndexAllocatorEntry
    {
        internal int Pos; // Position where the memory of this slot is
        internal int Length; // Length of the memory allocated to this slot
        internal int Next; // Next and Prev define the order of the slots in the buffer
        internal int Prev;
        internal bool Free;  // Whether this is a free slot
    }

    internal class IndexAllocator
    {
        private const int k_NotSet = -1;
        private readonly int m_MaxSlot; // Maximum number of sections (free or not) in the buffer
        private readonly int m_BufferSize; // Size of the buffer we allocated into
        private int m_LastSlot = 0; // Last allocated slot
        private IndexAllocatorEntry[] m_Slots; // Array of slots
        private int[] m_IndexToSlot; // Mapping from the client's index to the slot index

        internal IndexAllocator(int bufferSize, int maxSlot)
        {
            m_MaxSlot = maxSlot;
            m_BufferSize = bufferSize;
            m_Slots = new IndexAllocatorEntry[m_MaxSlot];
            m_IndexToSlot = new int[m_MaxSlot];
            Reset();
        }

        /// <summary>
        /// Reset this IndexAllocator to an empty one, with the same sized buffer and slots
        /// </summary>
        internal void Reset()
        {
            // todo: could be made faster, for example by having a last index
            // and not needing valid stuff past it
            for (int i = 0; i < m_MaxSlot; i++)
            {
                m_Slots[i].Free = true;
                m_Slots[i].Next = i + 1;
                m_Slots[i].Prev = i - 1;
                m_Slots[i].Pos = m_BufferSize;
                m_Slots[i].Length = 0;

                m_IndexToSlot[i] = k_NotSet;
            }

            m_Slots[0].Pos = 0;
            m_Slots[0].Length = m_BufferSize;
            m_Slots[0].Prev = k_NotSet;
            m_Slots[m_MaxSlot - 1].Next = k_NotSet;
        }

        /// <summary>
        /// Returns the amount of memory used
        /// </summary>
        /// <returns>
        /// Returns the amount of memory used, starting at 0, ending after the last used slot
        /// </returns>
        internal int Range
        {
            get
            {
                // when the whole buffer is free, m_LastSlot points to an empty slot
                if (m_Slots[m_LastSlot].Free)
                {
                    return 0;
                }
                // otherwise return the end of the last slot used
                return m_Slots[m_LastSlot].Pos + m_Slots[m_LastSlot].Length;
            }
        }

        /// <summary>
        /// Allocate a slot with "size" position, for index "index"
        /// </summary>
        /// <param name="index">The client index to identify this. Used in Deallocate to identify which slot</param>
        /// <param name="size">The size required. </param>
        /// <param name="pos">Returns the position to use in the buffer </param>
        /// <returns>
        /// true if successful, false is there isn't enough memory available or no slots are large enough
        /// </returns>
        internal bool Allocate(int index, int size, out int pos)
        {
            pos = 0;
            // size must be positive, index must be within range
            if (size < 0 || index < 0 || index >= m_MaxSlot)
            {
                return false;
            }

            // refuse allocation if the index is already in use
            if (m_IndexToSlot[index] != k_NotSet)
            {
                return false;
            }

            // todo: this is the slowest part
            // improvement 1: list of free blocks (minor)
            // improvement 2: heap of free blocks
            for (int i = 0; i < m_MaxSlot; i++)
            {
                if (m_Slots[i].Free && m_Slots[i].Length >= size)
                {
                    m_IndexToSlot[index] = i;

                    int leftOver = m_Slots[i].Length - size;
                    int next = m_Slots[i].Next;
                    if (m_Slots[next].Free)
                    {
                        m_Slots[next].Pos -= leftOver;
                        m_Slots[next].Length += leftOver;
                    }
                    else
                    {
                        int add = MoveSlotAfter(i);

                        m_Slots[add].Pos = m_Slots[i].Pos + size;
                        m_Slots[add].Length = m_Slots[i].Length - size;
                    }

                    m_Slots[i].Free = false;
                    m_Slots[i].Length = size;

                    pos = m_Slots[i].Pos;

                    // if we allocate past the current range, we are the last slot
                    if (m_Slots[i].Pos + m_Slots[i].Length > Range)
                    {
                        m_LastSlot = i;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Deallocate a slot
        /// </summary>
        /// <param name="index">The client index to identify this. Same index used in Allocate </param>
        /// <returns>
        /// true if successful, false is there isn't an allocated slot at this index
        /// </returns>
        internal bool Deallocate(int index)
        {
            // size must be positive, index must be within range
            if (index < 0 || index >= m_MaxSlot)
            {
                return false;
            }

            int slot = m_IndexToSlot[index];

            if (slot == k_NotSet)
            {
                return false;
            }

            if (m_Slots[slot].Free)
            {
                return false;
            }

            m_Slots[slot].Free = true;

            int prev = m_Slots[slot].Prev;
            int next = m_Slots[slot].Next;

            // if previous slot was free, merge and grow
            if (prev != k_NotSet && m_Slots[prev].Free)
            {
                m_Slots[prev].Length += m_Slots[slot].Length;
                m_Slots[slot].Length = 0;

                // if the slot we're merging was the last one, the last one is now the one we merged with
                if (slot == m_LastSlot)
                {
                    m_LastSlot = prev;
                }

                // todo: verify what this does on full or nearly full cases
                MoveSlotToEnd(slot);
                slot = prev;
            }

            next = m_Slots[slot].Next;

            // merge with next slot if it is free
            if (next != k_NotSet && m_Slots[next].Free)
            {
                m_Slots[slot].Length += m_Slots[next].Length;
                m_Slots[next].Length = 0;
                MoveSlotToEnd(next);
            }

            // if we just deallocate the last one, we need to move last back
            if (slot == m_LastSlot)
            {
                m_LastSlot = m_Slots[m_LastSlot].Prev;
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

        // Take a slot at the end and link it to go just after "slot".
        // Used when allocating part of a slot and we need an entry for the rest
        // Returns the slot that was picked
        private int MoveSlotAfter(int slot)
        {
            int ret = m_Slots[m_MaxSlot - 1].Prev;
            int p0 = m_Slots[ret].Prev;

            m_Slots[p0].Next = m_MaxSlot - 1;
            m_Slots[m_MaxSlot - 1].Prev = p0;

            int p1 = m_Slots[slot].Next;
            m_Slots[slot].Next = ret;
            m_Slots[p1].Prev = ret;

            m_Slots[ret].Prev = slot;
            m_Slots[ret].Next = p1;

            return ret;
        }

        // Move the slot "slot" to the end of the list.
        // Used when merging two slots, that gives us an extra entry at the end
        private void MoveSlotToEnd(int slot)
        {
            // if we're already there
            if (m_Slots[slot].Next == k_NotSet)
            {
                return;
            }

            int prev = m_Slots[slot].Prev;
            int next = m_Slots[slot].Next;

            m_Slots[prev].Next = next;
            if (next != k_NotSet)
            {
                m_Slots[next].Prev = prev;
            }

            int p0 = m_Slots[m_MaxSlot - 1].Prev;

            m_Slots[p0].Next = slot;
            m_Slots[slot].Next = m_MaxSlot - 1;

            m_Slots[m_MaxSlot - 1].Prev = slot;
            m_Slots[slot].Prev = p0;

            m_Slots[slot].Pos = m_BufferSize;
        }

        // runs a bunch of consistency check on the Allocator
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
                    pos = m_Slots[pos].Next;
                    if (pos == k_NotSet)
                    {
                        break;
                    }
                }
                else
                {
                    pos = 0;
                }

                if (m_Slots[pos].Prev != prev)
                {
                    // the previous is not correct
                    return false;
                }

                if (m_Slots[pos].Length < 0)
                {
                    // Length should be positive
                    return false;
                }

                if (prev != k_NotSet && m_Slots[prev].Free && m_Slots[pos].Free && m_Slots[pos].Length > 0)
                {
                    // should not have two consecutive free slots
                    return false;
                }

                if (m_Slots[pos].Pos != total)
                {
                    // slots should all line up nicely
                    return false;
                }

                if (!m_Slots[pos].Free)
                {
                    endPos = m_Slots[pos].Pos + m_Slots[pos].Length;
                }

                total += m_Slots[pos].Length;
                count++;

            } while (pos != k_NotSet);

            if (count != m_MaxSlot)
            {
                // some slots were lost
                return false;
            }

            if (total != m_BufferSize)
            {
                // total buffer should be accounted for
                return false;
            }

            if (endPos != Range)
            {
                // end position should match reported end position
                return false;
            }

            return true;
        }

        // Debug display the allocator structure
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
                if (m_Slots[pos].Length == 0 && prevEmpty)
                {
                    // don't display repetitive empty slots
                }
                else
                {
                    logMessage += string.Format("{0}:{1}, {2} ({3}) \n", m_Slots[pos].Pos, m_Slots[pos].Length,
                        m_Slots[pos].Free ? "Free" : "Used", pos);
                    if (m_Slots[pos].Length == 0)
                    {
                        prevEmpty = true;
                    }
                    else
                    {
                        prevEmpty = false;
                    }
                }

                pos = m_Slots[pos].Next;
            } while (pos != k_NotSet && !seen[pos]);

            logMessage += string.Format("{0} Total entries\n", count);

            Debug.Log(logMessage);
        }
    }
}
