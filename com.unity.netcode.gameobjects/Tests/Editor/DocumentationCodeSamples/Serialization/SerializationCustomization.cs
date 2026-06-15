using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;

namespace DocumentationCodeSamples
{
    #region HealthStruct
    /// <summary>Container for storing health data for a player or item.</summary>
    public struct Health
    {
        /// <summary>
        /// The maximum health that this player or item can have.
        /// This is unlikely to change often.
        /// </summary>
        public uint MaxHealth;

        /// <summary>
        /// The current level of health that this player or item has.
        /// This is likely to change regularly.
        /// </summary>
        public int CurrentHealth;
    }
    #endregion

    #region FastBuffer
    /// <summary>Tells the Netcode how to serialize and deserialize our custom type.</summary>
    // The class name doesn't matter here.
    public static class FastBufferExtensions
    {
        /// <summary>
        /// Extension method to override the serialization for a custom type.
        /// </summary>
        /// <param name="writer">Buffer to write values into.</param>
        /// <param name="health">The type to customize or override.</param>
        public static void WriteValueSafe(this FastBufferWriter writer, in Health health)
        {
            writer.WriteValueSafe(health.MaxHealth);
            writer.WriteValueSafe(health.CurrentHealth);
        }

        /// <summary>
        /// Extension method to override the de-serialization for a custom type.
        /// </summary>
        /// <param name="reader">Buffer to read values from.</param>
        /// <param name="health">The type to customize or override.</param>
        public static void ReadValueSafe(this FastBufferReader reader, out Health health)
        {
            reader.ReadValueSafe(out uint max);
            reader.ReadValueSafe(out int current);
            health = new Health { MaxHealth = max, CurrentHealth = current };
        }
    }
    #endregion

    #region BufferSerializer
    /// <summary>Tells the <see cref="BufferSerializer{TReaderWriter}"/> how to serialize and deserialize our custom type.</summary>
    // The class name doesn't matter here.
    public static class BufferSerializerExtensions
    {
        /// <summary>
        /// Extension method to override bi-directional serialization for a custom type.
        /// </summary>
        /// <param name="serializer">Bi-directional serial</param>
        /// <param name="health">The type to customize or override.</param>
        /// <typeparam name="TReaderWriter">Boilerplate syntax to enable the bi-directional serialization.</typeparam>
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
