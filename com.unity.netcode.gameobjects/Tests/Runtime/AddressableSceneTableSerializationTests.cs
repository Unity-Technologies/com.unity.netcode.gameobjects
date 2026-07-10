using System;
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Verifies the Addressable scene table travels with <see cref="SceneEventData"/> on the wire and that
    /// relocating it to the end of the payload (after every fixed field the Distributed Authority relay
    /// parses) preserves both the table contents and the surrounding fixed fields. The two event types with
    /// a trailing, greedily-read blob (<see cref="SceneEventType.Load"/> and
    /// <see cref="SceneEventType.Synchronize"/>) write the table just before that blob; every other type
    /// writes it last. The version gate must keep the bytes out of the payload entirely for older peers.
    /// </summary>
    internal class AddressableSceneTableSerializationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        // Two distinct address styles (full asset path and a plain key) to make sure both survive verbatim.
        private const string k_AddressA = "Assets/Scenes/AddressableSceneA.unity";
        private const string k_AddressB = "AddressableSceneKeyB";

        private const int k_Version1 = SceneEventData.AddressableSceneTableVersion;
        private const int k_LegacyVersion = SceneEventData.AddressableSceneTableVersion - 1;

        private uint m_HashA;
        private uint m_HashB;

        private NetworkSceneManager ServerSceneManager => m_ServerNetworkManager.SceneManager;
        private NetworkSceneManager ClientSceneManager => m_ClientNetworkManagers[0].SceneManager;

        protected override IEnumerator OnServerAndClientsConnected()
        {
            // Register the Addressable scenes on the server only, so any entry that later shows up on the
            // client can only have arrived via the serialized table.
            m_HashA = ServerSceneManager.AddressableScenes.Register(k_AddressA);
            m_HashB = ServerSceneManager.AddressableScenes.Register(k_AddressB);
            yield return base.OnServerAndClientsConnected();
        }

        /// <summary>
        /// Serializes a server-side <see cref="SceneEventData"/> at <paramref name="version"/> and deserializes
        /// it into a fresh client-side instance, returning the received copy. The caller owns disposal.
        /// </summary>
        private SceneEventData RoundTrip(SceneEventType sceneEventType, int version, Action<SceneEventData> configure)
        {
            var sent = new SceneEventData(m_ServerNetworkManager)
            {
                SceneEventType = sceneEventType,
            };
            configure?.Invoke(sent);

            var writer = new FastBufferWriter(1024, Allocator.Temp, int.MaxValue);
            try
            {
                sent.Serialize(writer, version);
                var reader = new FastBufferReader(writer, Allocator.Temp);
                try
                {
                    var received = new SceneEventData(m_ClientNetworkManagers[0]);
                    // Load/Synchronize copy their trailing blob into a persistent InternalBuffer during
                    // Deserialize, so the received instance is safe to use after the source reader is disposed.
                    received.Deserialize(reader, version);
                    return received;
                }
                finally
                {
                    reader.Dispose();
                }
            }
            finally
            {
                writer.Dispose();
            }
        }

        private int SerializedLength(SceneEventType sceneEventType, int version, Action<SceneEventData> configure)
        {
            var sent = new SceneEventData(m_ServerNetworkManager)
            {
                SceneEventType = sceneEventType,
            };
            configure?.Invoke(sent);
            var writer = new FastBufferWriter(1024, Allocator.Temp, int.MaxValue);
            try
            {
                sent.Serialize(writer, version);
                return writer.Length;
            }
            finally
            {
                writer.Dispose();
            }
        }

        private void AssertClientResolvesServerTable()
        {
            Assert.That(ClientSceneManager.AddressableScenes.TryGetAddress(m_HashA, out var addressA), Is.True,
                "Client did not receive Addressable table entry A from the serialized event.");
            Assert.That(addressA, Is.EqualTo(k_AddressA));
            Assert.That(ClientSceneManager.AddressableScenes.TryGetAddress(m_HashB, out var addressB), Is.True,
                "Client did not receive Addressable table entry B from the serialized event.");
            Assert.That(addressB, Is.EqualTo(k_AddressB));
        }

        // A Load event writes the table immediately before the greedily-read scene-placed-object blob. This
        // is the case that would silently corrupt if the table were written after that blob, so we assert both
        // the table and every fixed field survive the round trip.
        [Test]
        public void Version1_LoadEvent_TableTransfersAndFixedFieldsSurvive()
        {
            var progressId = Guid.NewGuid();
            var sceneHandle = new NetworkSceneHandle(23456, true);
            const uint sceneHash = 987654u;

            var received = RoundTrip(SceneEventType.Load, k_Version1, data =>
            {
                data.LoadSceneMode = LoadSceneMode.Additive;
                data.SceneEventProgressId = progressId;
                data.SceneHash = sceneHash;
                data.SceneHandle = sceneHandle;
            });

            try
            {
                Assert.That(received.SceneEventType, Is.EqualTo(SceneEventType.Load));
                Assert.That(received.LoadSceneMode, Is.EqualTo(LoadSceneMode.Additive));
                Assert.That(received.SceneEventProgressId.Value, Is.EqualTo(progressId));
                Assert.That(received.SceneHash, Is.EqualTo(sceneHash));
                Assert.That(received.SceneHandle, Is.EqualTo(sceneHandle));
                AssertClientResolvesServerTable();
            }
            finally
            {
                // Load captured a persistent InternalBuffer for the trailing (empty) scene-placed-object blob.
                received.Dispose();
            }
        }

        // ActiveSceneChanged has no trailing blob, so the table is written last. Verify the fixed field that
        // precedes it and the table both round-trip.
        [Test]
        public void Version1_ActiveSceneChangedEvent_TableWrittenLast()
        {
            const uint activeSceneHash = 424242u;

            var received = RoundTrip(SceneEventType.ActiveSceneChanged, k_Version1, data =>
            {
                data.ActiveSceneHash = activeSceneHash;
            });

            try
            {
                Assert.That(received.SceneEventType, Is.EqualTo(SceneEventType.ActiveSceneChanged));
                Assert.That(received.ActiveSceneHash, Is.EqualTo(activeSceneHash));
                AssertClientResolvesServerTable();
            }
            finally
            {
                received.Dispose();
            }
        }

        // With a legacy (pre-table) negotiated version the table must not be written or read: the client
        // registry gains nothing, the fixed fields still round-trip, and the payload is strictly smaller than
        // the version that carries the table.
        [Test]
        public void LegacyVersion_OmitsTable_AndIsShorterThanVersion1()
        {
            var progressId = Guid.NewGuid();
            var sceneHandle = new NetworkSceneHandle(13579, true);
            const uint sceneHash = 112233u;

            void Configure(SceneEventData data)
            {
                data.LoadSceneMode = LoadSceneMode.Single;
                data.SceneEventProgressId = progressId;
                data.SceneHash = sceneHash;
                data.SceneHandle = sceneHandle;
            }

            // The version that carries a non-empty table must be larger than the one that omits it entirely.
            var legacyLength = SerializedLength(SceneEventType.Load, k_LegacyVersion, Configure);
            var version1Length = SerializedLength(SceneEventType.Load, k_Version1, Configure);
            Assert.That(version1Length, Is.GreaterThan(legacyLength),
                "The table-carrying version should serialize more bytes than the legacy version.");

            var clientEntriesBefore = ClientSceneManager.AddressableScenes.Count;

            var received = RoundTrip(SceneEventType.Load, k_LegacyVersion, Configure);
            try
            {
                // No table bytes were read, so nothing new was registered on the client (order-independent).
                Assert.That(ClientSceneManager.AddressableScenes.Count, Is.EqualTo(clientEntriesBefore),
                    "A legacy-version event must not register any Addressable table entries on the client.");

                // The fixed fields still survive with the table omitted.
                Assert.That(received.LoadSceneMode, Is.EqualTo(LoadSceneMode.Single));
                Assert.That(received.SceneEventProgressId.Value, Is.EqualTo(progressId));
                Assert.That(received.SceneHash, Is.EqualTo(sceneHash));
                Assert.That(received.SceneHandle, Is.EqualTo(sceneHandle));
            }
            finally
            {
                received.Dispose();
            }
        }
    }
}
