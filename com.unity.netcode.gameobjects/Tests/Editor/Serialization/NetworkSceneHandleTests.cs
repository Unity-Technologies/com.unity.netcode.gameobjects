using NUnit.Framework;
using Unity.Collections;

namespace Unity.Netcode.EditorTests
{
    internal class NetworkSceneHandleTests
    {
        [Test]
        public void NetworkSceneHandleSerializationTest()
        {
            var handle = new NetworkSceneHandle(1234, true);

            using var writer = new FastBufferWriter(sizeof(ulong), Allocator.Temp);
            Assert.That(writer.Position, Is.EqualTo(0), "Writer position should be zero");

            writer.WriteValue(handle);

            Assert.That(writer.Position, Is.EqualTo(sizeof(ulong)), "Writer position should not be beyond size");

            var reader = new FastBufferReader(writer, Allocator.Temp);
            Assert.That(reader.Position, Is.EqualTo(0), "Reader position should be zero");
            reader.ReadValue(out NetworkSceneHandle deserializedHandle);
            Assert.That(writer.Position, Is.EqualTo(sizeof(ulong)), "Reader position should not be beyond size");

            Assert.AreEqual(handle, deserializedHandle);

            // Now serialize a list of SceneHandles
            var handles = new NetworkSceneHandle[] { handle, new NetworkSceneHandle(4567, true), new NetworkSceneHandle(7890, true) };

            using var listWriter = new FastBufferWriter(1024, Allocator.Temp);

            Assert.That(listWriter.Position, Is.EqualTo(0), "Writer position should be zero");

            listWriter.WriteValue(handles);

            var expectedSize = sizeof(int) + (sizeof(ulong) * handles.Length);
            Assert.That(listWriter.Position, Is.EqualTo(expectedSize), "Writer position should not be beyond size");

            var listReader = new FastBufferReader(listWriter, Allocator.Temp);
            Assert.That(listReader.Position, Is.EqualTo(0), "Reader position should be zero");
            listReader.ReadValue(out NetworkSceneHandle[] deserializedHandleList);
            Assert.That(listReader.Position, Is.EqualTo(expectedSize), "Reader position should not be beyond expected size");

            Assert.AreEqual(handles, deserializedHandleList);
        }
    }
}
