using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Unity.Netcode.EditorTests
{
    public class SnapshotTests
    {
        private SnapshotSystem m_SendSnapshot;
        private SnapshotSystem m_RecvSnapshot;

        private NetworkTimeSystem m_TimeSystem;
        private NetworkTickSystem m_TickSystem;

        private int m_SpawnedObjectCount;
        private int m_DespawnedObjectCount;
        private int m_NextSequence;
        private int m_TicksToRun = 20;
        private uint m_TicksPerSec = 15;

        private bool m_ExpectSpawns;
        private bool m_ExpectDespawns;

        public void Prepare()
        {
            m_TickSystem = new NetworkTickSystem(m_TicksPerSec, 0.0, 0.0);
            m_TimeSystem = new NetworkTimeSystem(0.2, 0.2, 1.0);

            PrepareSendSideSnapshot();
            PrepareRecvSideSnapshot();
        }

        public void AdvanceOneTick()
        {
            m_TimeSystem.Advance(1.0f / m_TicksPerSec);
            m_TickSystem.UpdateTick(m_TimeSystem.LocalTime, m_TimeSystem.ServerTime);
            m_SendSnapshot.NetworkUpdate(NetworkUpdateStage.EarlyUpdate);
        }

        internal int SpawnObject(SnapshotSpawnCommand command)
        {
            Debug.Log("Object spawned");
            m_SpawnedObjectCount++;
            return 0;
        }

        internal int DespawnObject(SnapshotDespawnCommand command)
        {
            Debug.Log("Object despawned");
            m_DespawnedObjectCount++;
            return 0;
        }

        internal int SendMessage(in SnapshotDataMessage message, NetworkDelivery delivery, ulong clientId)
        {
            Debug.Log("Snapshot Message sent");

            Debug.Assert(message.Ack.LastReceivedSequence == 0); // we're not ack'ing anything, so those should stay 0
            Debug.Assert(message.Ack.ReceivedSequenceMask == 0);
            Debug.Assert(message.Sequence == m_NextSequence); // sequence has to be the expected one

            if (m_ExpectSpawns)
            {
                Debug.Assert(message.Spawns.Length > 1); // there has to be multiple spawns per SnapshotMessage
            }
            else
            {
                Debug.Assert(message.Spawns.Length == 0); // Spawns were not expected
            }

            if (m_ExpectDespawns)
            {
                Debug.Assert(message.Despawns.Length > 1); // there has to be multiple despawns per SnapshotMessage
            }
            else
            {
                Debug.Assert(message.Despawns.IsEmpty); // this test should not have despawns
            }

            Debug.Assert(message.Entries.Length == 0);

            m_NextSequence++;

            using FastBufferWriter writer = new FastBufferWriter(1024, Allocator.Temp);
            message.Serialize(writer);
            using FastBufferReader reader = new FastBufferReader(writer, Allocator.Temp);
            var context = new NetworkContext{SenderId = 0, Timestamp = 0.0f, SystemOwner = m_RecvSnapshot};
            SnapshotDataMessage.Receive(reader, context);

            return 0;
        }

        private void PrepareSendSideSnapshot()
        {
            var config = new NetworkConfig();

            config.UseSnapshotDelta = false;
            config.UseSnapshotSpawn = true;

            m_SendSnapshot = new SnapshotSystem(null, config, m_TickSystem);

            m_SendSnapshot.IsServer = true;
            m_SendSnapshot.IsConnectedClient = false;
            m_SendSnapshot.ServerClientId = 0;
            m_SendSnapshot.ConnectedClientsId.Clear();
            m_SendSnapshot.ConnectedClientsId.Add(1);
            m_SendSnapshot.MockSendMessage = SendMessage;
            m_SendSnapshot.MockSpawnObject = SpawnObject;
            m_SendSnapshot.MockDespawnObject = DespawnObject;
        }

        private void PrepareRecvSideSnapshot()
        {
            var config = new NetworkConfig();

            config.UseSnapshotDelta = false;
            config.UseSnapshotSpawn = true;

            m_RecvSnapshot = new SnapshotSystem(null, config, m_TickSystem);

            m_RecvSnapshot.IsServer = true;
            m_RecvSnapshot.IsConnectedClient = false;
            m_RecvSnapshot.ServerClientId = 0;
            m_RecvSnapshot.ConnectedClientsId.Clear();
            m_RecvSnapshot.ConnectedClientsId.Add(1);
            m_RecvSnapshot.MockSendMessage = SendMessage;
            m_RecvSnapshot.MockSpawnObject = SpawnObject;
            m_RecvSnapshot.MockDespawnObject = DespawnObject;
        }

        private void SendSpawnToSnapshot(ulong objectId)
        {
            SnapshotSpawnCommand command = default;
            // identity
            command.NetworkObjectId = objectId;
            // archetype
            command.GlobalObjectIdHash = 0;
            command.IsSceneObject = true;
            // parameters
            command.IsPlayerObject = false;
            command.OwnerClientId = 0;
            command.ParentNetworkId = 0;
            command.ObjectPosition = default;
            command.ObjectRotation = default;
            command.ObjectScale = new Vector3(1.0f, 1.0f, 1.0f);

            command.TargetClientIds = new List<ulong> { 1 };
            m_SendSnapshot.Spawn(command);
        }

        private void SendDespawnToSnapshot(ulong objectId)
        {
            SnapshotDespawnCommand command = default;
            // identity
            command.NetworkObjectId = objectId;

            command.TargetClientIds = new List<ulong> { 1 };
            m_SendSnapshot.Despawn(command);
        }

        [Test]
        public void TestSnapshotSpawn()
        {
            Prepare();

            m_SpawnedObjectCount = 0;
            m_NextSequence = 0;
            m_ExpectSpawns = true;
            m_ExpectDespawns = false;

            // spawns one more than current buffer size
            var objectsToSpawn= m_SendSnapshot.SpawnsBufferCount + 1;

            for (int i = 0; i < objectsToSpawn; i++)
            {
                SendSpawnToSnapshot((ulong)i);
                Debug.Log($"{m_SpawnedObjectCount} spawned objects");
            }

            for (int i = 0; i < m_TicksToRun; i++)
            {
                AdvanceOneTick();
            }

            Debug.Assert(m_SpawnedObjectCount == objectsToSpawn);
            Debug.Assert(m_SendSnapshot.SpawnsBufferCount > objectsToSpawn); // spawn buffer should have grown
        }

        [Test]
        public void TestSnapshotSpawnDespawns()
        {
            Prepare();

            m_SpawnedObjectCount = 0;
            m_NextSequence = 0;
            m_ExpectSpawns = true;
            m_ExpectDespawns = false;

            // spawns one more than current buffer size
            var objectsToSpawn = 10;

            for (int i = 0; i < objectsToSpawn; i++)
            {
                SendSpawnToSnapshot((ulong)i);
                Debug.Log($"{m_SpawnedObjectCount} spawned objects");
            }

            for (int i = 0; i < m_TicksToRun; i++)
            {
                AdvanceOneTick();
            }

            for (int i = 0; i < objectsToSpawn; i++)
            {
                SendDespawnToSnapshot((ulong)i);
            }

            m_ExpectSpawns = true; // the un'acked spawns will still be present
            m_ExpectDespawns = true;

            for (int i = 0; i < m_TicksToRun; i++)
            {
                AdvanceOneTick();
            }

            Debug.Assert(m_DespawnedObjectCount == objectsToSpawn);
        }
    }
}

