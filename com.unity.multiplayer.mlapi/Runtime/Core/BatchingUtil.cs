using MLAPI.Serialization.Pooled;
using MLAPI.Serialization;

namespace MLAPI
{
    class BatchUtil
    {
        public void PushLength(int length, ref PooledBitWriter writer)
        {
            // supports lengths up to 255^2
            if (length < 255)
            {
                writer.WriteByte((byte)length); // write the amounts of bytes that are coming up
            }
            else
            {
                writer.WriteByte(255); // mark larger size
                writer.WriteByte((byte)(length % 255)); // write the length modulo 255
                writer.WriteByte((byte)(length / 255)); // write the length divided by 255
            }
        }

        public int PopLength(in BitStream messageStream)
        {
            int length = 0;
            byte read = (byte)messageStream.ReadByte();
            if (read != 255)
            {
                return read;
            }
            length += messageStream.ReadByte();
            length += messageStream.ReadByte() * 255;

            return length;
        }
    }
}
