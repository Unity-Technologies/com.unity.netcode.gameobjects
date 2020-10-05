using System.IO;
using System.Reflection;
using MLAPI.Serialization.Pooled;

namespace MLAPI.Serialization
{
    /// <summary>
    /// AutoBitWritable implements IBitWritable and automatically serializes fields using reflection
    /// </summary>
    public abstract class AutoBitWritable : IBitWritable
    {        
        /// <summary>
        /// Writes the contents of the type instance to the stream
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        public virtual void Write(Stream stream)
        {
            FieldInfo[] fields = SerializationManager.GetFieldsForType(GetType());

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
            FieldInfo[] fields = SerializationManager.GetFieldsForType(GetType());

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