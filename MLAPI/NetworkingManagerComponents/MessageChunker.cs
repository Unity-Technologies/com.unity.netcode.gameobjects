using System;
using System.Collections.Generic;
using System.IO;

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

        public static byte[] GetMessageOrdered(ref List<byte[]> chunks, int chunkSize = -1)
        {
            if (chunks.Count == 0)
                return new byte[0];
            if (chunkSize == -1)
                chunkSize = chunks[0].Length - 4;

            uint lastIndex = 0;
            uint messageSize = 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                uint chunkIndex =  BitConverter.ToUInt32(chunks[i], 0);
                if (chunkIndex <= lastIndex)
                    throw new ArgumentException("Chunks not in order");
                lastIndex = chunkIndex;
                messageSize += Convert.ToUInt32(chunks[i].Length - 4);
            }
            byte[] message = new byte[messageSize];
            for (int i = 0; i < chunks.Count; i++)
            {
                Array.Copy(chunks[i], 3, message, i * chunkSize, chunkSize);
            }
            return message;
        }
    }
}
