using NUnit.Framework;
using Unity.Collections;

namespace Unity.Netcode.EditorTests.Documentation.Serialization
{
    #region HealthStruct
    public struct Health
    {
        public uint MaxHealth;
        public int CurrentHealth;
    }
    #endregion

    #region FastBuffer
    // Tells the Netcode how to serialize and deserialize our custom type.
    // The class name doesn't matter here.
    public static class FastBufferExtensions
    {
        public static void WriteValueSafe(this FastBufferWriter writer, in Health health)
        {
            writer.WriteValueSafe(health.MaxHealth);
            writer.WriteValueSafe(health.CurrentHealth);
        }

        public static void ReadValueSafe(this FastBufferReader reader, out Health health)
        {
            reader.ReadValueSafe(out uint max);
            reader.ReadValueSafe(out int current);
            health = new Health { MaxHealth = max, CurrentHealth = current };
        }
    }
    #endregion

    #region BufferSerializer
    // The class name doesn't matter here.
    public static class BufferSerializerExtensions
    {
        public static void SerializeValue<TReaderWriter>(this BufferSerializer<TReaderWriter> serializer, ref Health health) where TReaderWriter : IReaderWriter
        {
            // Because the BufferSerializer already knows how to read and write the primitive types
            // We can use the existing BufferSerializer serialization.
            serializer.SerializeValue(ref health.MaxHealth);
            serializer.SerializeValue(ref health.CurrentHealth);
        }
    }
    #endregion

    internal class TestSerializationDocs
    {
        [Test]
        public void TestFastBufferSerialization()
        {
            var healthToTest = new Health { MaxHealth = 123, CurrentHealth = 89 };
            var expected = healthToTest;

            using var writer = new FastBufferWriter(256, Allocator.Temp, int.MaxValue);
            writer.WriteValueSafe(healthToTest);

            using var reader = new FastBufferReader(writer, Allocator.None);
            reader.ReadValueSafe(out Health readHealth);

            Assert.AreEqual(expected.MaxHealth, readHealth.MaxHealth);
            Assert.AreEqual(expected.CurrentHealth, readHealth.CurrentHealth);
        }

        [Test]
        public void TestBufferSerializerSerialization()
        {
            var healthToTest = new Health { MaxHealth = 456, CurrentHealth = 78 };
            var expected = healthToTest;

            using var writer = new FastBufferWriter(256, Allocator.Temp, int.MaxValue);
            var bufferWriter = new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
            bufferWriter.SerializeValue(ref healthToTest);

            using var tempReader = new FastBufferReader(bufferWriter.GetFastBufferWriter(), Allocator.None);
            var bufferReader = new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(tempReader));
            var readHealth = new Health();
            bufferReader.SerializeValue(ref readHealth);

            Assert.AreEqual(expected.MaxHealth, readHealth.MaxHealth);
            Assert.AreEqual(expected.CurrentHealth, readHealth.CurrentHealth);

        }
    }
}
