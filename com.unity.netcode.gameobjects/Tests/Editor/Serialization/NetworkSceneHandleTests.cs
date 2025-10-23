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

            writer.WriteNetworkSerializable(handle);
#if SCENE_MANAGEMENT_SCENE_HANDLE_MUST_USE_ULONG
            Assert.That(writer.Position, Is.EqualTo(sizeof(ulong)), $"Writer position should not be beyond size! Expected: {sizeof(ulong)} Actual: {writer.Position}");
#else
            Assert.That(writer.Position, Is.EqualTo(sizeof(int)), $"Writer position should not be beyond size! Expected: {sizeof(int)} Actual: {writer.Position}");
#endif

            var reader = new FastBufferReader(writer, Allocator.Temp);
            Assert.That(reader.Position, Is.EqualTo(0), "Reader position should be zero");
            reader.ReadNetworkSerializable(out NetworkSceneHandle deserializedHandle);
#if SCENE_MANAGEMENT_SCENE_HANDLE_MUST_USE_ULONG
            Assert.That(reader.Position, Is.EqualTo(sizeof(ulong)), $"Reader position should not be beyond size! Expected: {sizeof(ulong)} Actual: {reader.Position}");
#else
            Assert.That(reader.Position, Is.EqualTo(sizeof(int)), $"Reader position should not be beyond size! Expected: {sizeof(int)} Actual: {reader.Position}");
#endif

            Assert.AreEqual(handle, deserializedHandle);

            // Now serialize a list of SceneHandles
            var handles = new NetworkSceneHandle[] { handle, new NetworkSceneHandle(4567, true), new NetworkSceneHandle(7890, true) };

            using var listWriter = new FastBufferWriter(1024, Allocator.Temp);

            Assert.That(listWriter.Position, Is.EqualTo(0), "Writer position should be zero");

            listWriter.WriteNetworkSerializable(handles);
#if SCENE_MANAGEMENT_SCENE_HANDLE_MUST_USE_ULONG
            var expectedSize = sizeof(int) + (sizeof(ulong) * handles.Length);
#else
            var expectedSize = sizeof(int) + (sizeof(int) * handles.Length);
#endif
            Assert.That(listWriter.Position, Is.EqualTo(expectedSize), $"Writer position should not be beyond size! Expected: {expectedSize} Actual: {listWriter.Position}");

            var listReader = new FastBufferReader(listWriter, Allocator.Temp);
            Assert.That(listReader.Position, Is.EqualTo(0), "Reader position should be zero");
            listReader.ReadNetworkSerializable(out NetworkSceneHandle[] deserializedHandleList);
            Assert.That(listReader.Position, Is.EqualTo(expectedSize), $"Reader position should not be beyond expected size! Expected: {expectedSize} Actual: {listReader.Position}");

            Assert.AreEqual(handles, deserializedHandleList);
        }
    }
}
