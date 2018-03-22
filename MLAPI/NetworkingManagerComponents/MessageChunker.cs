using System;
using System.Collections.Generic;

namespace MLAPI.NetworkingManagerComponents
{
    public static class MessageChunker
    {
        public static List<byte[]> GetChunkedMessage(ref byte[] message, int chunkSize)
        {
            List<byte[]> chunks = new List<byte[]>((int)Math.Ceiling((double)message.Length / chunkSize));
            //The extra 4 bytes is the chunkIndex   
            byte[] chunk = new byte[chunkSize + 4];

            for (int i = 0; i < message.Length; i++)
            {
                uint currentChunkIndex = (uint)Math.Floor((double)i / (double)chunkSize);
                int currentByteIndex = i % chunkSize;

                if (currentByteIndex == 0)
                {
                    byte[] chunkIndexBytes = BitConverter.GetBytes(currentChunkIndex);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(chunkIndexBytes);
                    chunk[0] = chunkIndexBytes[0]; 
                    chunk[1] = chunkIndexBytes[1];
                    chunk[2] = chunkIndexBytes[2];
                    chunk[3] = chunkIndexBytes[3];
                }
                chunk[4 + currentByteIndex] = message[i];
                if(currentByteIndex == (chunkSize -1) || i == (message.Length -1))
                {
                    //This is the last byte in the chunk
                    chunks.Add(chunk);
                }
            }
            return chunks;
        }

        public static bool HasMissingParts(ref List<byte[]> chunks, uint expectedChunksCount)
        {
            if (chunks.Count < expectedChunksCount)
                return true;

            HashSet<uint> chunkIndexes = new HashSet<uint>();
            uint duplicateCount = 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                uint chunkIndex = BitConverter.ToUInt32(chunks[i], 0);
                if (chunkIndexes.Contains(chunkIndex))
                    duplicateCount++;
                else
                    chunkIndexes.Add(chunkIndex);
            }
            return chunks.Count - duplicateCount != expectedChunksCount;
        }

        public static bool IsOrdered(ref List<byte[]> chunks)
        {
            uint lastChunkIndex = 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                uint chunkIndex = BitConverter.ToUInt32(chunks[i], 0);
                //This can't be right?
                if (chunkIndex <= lastChunkIndex)
                    lastChunkIndex++;
                else
                    return false;
            }
            return true;
        }

        public static bool HasDuplicates(ref List<byte[]> chunks, uint expectedChunksCount)
        {
            if (chunks.Count > expectedChunksCount)
                return true;

            HashSet<uint> chunkIndexes = new HashSet<uint>();
            for (int i = 0; i < chunks.Count; i++)
            {
                uint chunkIndex = BitConverter.ToUInt32(chunks[i], 0);
                if (chunkIndexes.Contains(chunkIndex))
                    return true;
                else
                    chunkIndexes.Add(chunkIndex);
            }
            return false;
        }


        public static byte[] GetMessageOrdered(ref List<byte[]> chunks, int chunkSize = -1)
        {
            if (chunks.Count == 0)
                return new byte[0];
            if (chunkSize == -1)
                chunkSize = chunks[0].Length - 4;

            uint messageSize = 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                messageSize += Convert.ToUInt32(chunks[i].Length - 4);
            }
            byte[] message = new byte[messageSize];
            for (int i = 0; i < chunks.Count; i++)
            {
                Array.Copy(chunks[i], 3, message, i * chunkSize, chunkSize);
            }
            return message;
        }

        public static byte[] GetMessageUnordered(ref List<byte[]> chunks, int chunkSize = -1)
        {
            if (chunks.Count == 0)
                return new byte[0];
            if (chunkSize == -1)
                chunkSize = chunks[0].Length - 4;

            uint messageSize = 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                messageSize += Convert.ToUInt32(chunks[i].Length - 4);
            }
            byte[] message = new byte[messageSize];
            uint nextIndex = 0;
            //Loop as many times as there are chunks.
            for (int i = 0; i < chunks.Count; i++)
            {
                //For each chunk, find the right chunk
                for (int j = 0; j < chunks.Count; j++)
                {
                    if(BitConverter.ToUInt32(chunks[j], 0) == nextIndex)
                    {
                        Array.Copy(chunks[j], 3, message, nextIndex * chunkSize, chunkSize);
                    }
                }
            }
            return message;
        }
    }
}
