using System;
using System.IO;
using System.Reflection;

namespace MLAPI.Serialization 
{
    [Obsolete("The IBitWritable interface has been replaced with the abstract BitWritable class", true)]
    public interface IBitWritable
    {
        [Obsolete("The IBitWritable interface has been replaced with the abstract BitWritable class", true)]
        void Read(Stream stream);
        [Obsolete("The IBitWritable interface has been replaced with the abstract BitWritable class", true)]
        void Write(Stream stream);
    }
    
    /// <summary>
    /// BitWritable is the base class for writable types
    /// </summary>
    public abstract class BitWritable
    {        
        /// <summary>
        /// Writes the contents of the type instance to the stream
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        public virtual void Write(Stream stream)
        {
            FieldInfo[] fields = SerializationHelper.GetFieldsForType(GetType());

            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    writer.WriteObjectPacked(fields[i].GetValue(this));
                }
            }
        }

        /// <summary>
        /// Reads the contents from the stream and applies it to the type instance
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        public virtual void Read(Stream stream)
        {
            FieldInfo[] fields = SerializationHelper.GetFieldsForType(GetType());

            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i].SetValue(this, reader.ReadObjectPacked(fields[i].FieldType));
                }
            }
        }
    }
}
