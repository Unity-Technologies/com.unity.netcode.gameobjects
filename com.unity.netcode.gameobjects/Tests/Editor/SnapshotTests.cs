using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using NUnit.Framework;
using Random = System.Random;

namespace Unity.Netcode.EditorTests
{
    public class SnapshotTests
    {
        private SnapshotSystem m_SendSnapshot;
        private SnapshotSystem m_RecvSnapshot;

        private NetworkTimeSystem m_SendTimeSystem;
        private NetworkTickSystem m_SendTickSystem;
        private NetworkTimeSystem m_RecvTimeSystem;
        private NetworkTickSystem m_RecvTickSystem;

        private int m_SpawnedObjectCount;
        private int m_DespawnedObjectCount;
        private int m_NextSequence;
        private uint m_TicksPerSec = 15;
        private int m_MinSpawns;
        private int m_MinDespawns;

        private bool m_ExpectSpawns;
        private bool m_ExpectDespawns;
        private bool m_LoseNextMessage;
        private bool m_PassBackResponses;

        public void Prepare()
        {
            PrepareSendSideSnapshot();
            PrepareRecvSideSnapshot();
        }

        public void AdvanceOneTickSendSide()
        {
            m_SendTimeSystem.Advance(1.0f / m_TicksPerSec);
            m_SendTickSystem.UpdateTick(m_SendTimeSystem.LocalTime, m_SendTimeSystem.ServerTime);
            m_SendSnapshot.NetworkUpdate(NetworkUpdateStage.EarlyUpdate);
            m_SendSnapshot.NetworkUpdate(NetworkUpdateStage.PostLateUpdate);
        }

        public void AdvanceOneTickRecvSide()
        {
            m_RecvTimeSystem.Advance(1.0f / m_TicksPerSec);
            m_RecvTickSystem.UpdateTick(m_RecvTimeSystem.LocalTime, m_RecvTimeSystem.ServerTime);
            m_RecvSnapshot.NetworkUpdate(NetworkUpdateStage.EarlyUpdate);
            m_SendSnapshot.NetworkUpdate(NetworkUpdateStage.PostLateUpdate);
        }

        public void AdvanceOneTick()
        {
            AdvanceOneTickSendSide();
            AdvanceOneTickRecvSide();
        }

        internal void PreSpawnObject(SnapshotSpawnCommand command, ulong srcClientId)
        {
            m_SpawnedObjectCount++;
        }

        internal void PostSpawnObject(SnapshotSpawnCommand command, ulong srcClientId)
        {

        }

        internal void DespawnObject(SnapshotDespawnCommand command, ulong srcClientId)
        {
            m_DespawnedObjectCount++;
        }

        internal void SimulateTransport(ref SnapshotDataMessage message)
        {
            message.ReadBuffer = new FastBufferReader(message.WriteBuffer, Allocator.Temp);
        }

        internal int SendMessage(SnapshotDataMessage message, ulong clientId)
        {
            m_NextSequence++;
            Debug.Log($"{m_MinSpawns} {m_MinDespawns} {m_ExpectSpawns} {m_ExpectDespawns}");

            SimulateTransport(ref message);

            if (!m_LoseNextMessage)
            {
                // pass to receiving Snapshot
                m_RecvSnapshot.HandleSnapshot(clientId, message);
            }

            return 0;
        }

        internal int SendMessageRecvSide(SnapshotDataMessage message, ulong clientId)
        {
            SimulateTransport(ref message);

            if (m_PassBackResponses)
            {
                // todo: pass back to sending Snapshot
                m_SendSnapshot.HandleSnapshot(clientId, message);
            }

            return 0;
        }


        private void PrepareSendSideSnapshot()
        {
            var config = new NetworkConfig();

            m_SendTickSystem = new NetworkTickSystem(m_TicksPerSec, 0.0, 0.0);
            m_SendTimeSystem = new NetworkTimeSystem(0.2, 0.2, 1.0);
            m_SendTimeSystem.Reset(0, 0.0);

            config.UseSnapshotDelta = false;

            m_SendSnapshot = new SnapshotSystem(null, config, m_SendTickSystem);

            m_SendSnapshot.IsServer = true;
            m_SendSnapshot.IsConnectedClient = false;
            m_SendSnapshot.ServerClientId = 0;
            m_SendSnapshot.ConnectedClientsId.Clear();
            m_SendSnapshot.ConnectedClientsId.Add(0);
            m_SendSnapshot.ConnectedClientsId.Add(1);
            m_SendSnapshot.SendMessage = SendMessage;
            m_SendSnapshot.PreSpawnObject = PreSpawnObject;
            m_SendSnapshot.PostSpawnObject = PostSpawnObject;
            m_SendSnapshot.DespawnObject = DespawnObject;
        }

        private void PrepareRecvSideSnapshot()
        {
            var config = new NetworkConfig();

            m_RecvTickSystem = new NetworkTickSystem(m_TicksPerSec, 0.0, 0.0);
            m_RecvTimeSystem = new NetworkTimeSystem(0.2, 0.2, 1.0);
            m_RecvTimeSystem.Reset(0, 0.0);

            config.UseSnapshotDelta = false;

            m_RecvSnapshot = new SnapshotSystem(null, config, m_RecvTickSystem);

            m_RecvSnapshot.IsServer = false;
            m_RecvSnapshot.IsConnectedClient = true;
            m_RecvSnapshot.ServerClientId = 0;
            m_SendSnapshot.ConnectedClientsId.Clear();
            m_SendSnapshot.ConnectedClientsId.Add(0);
            m_SendSnapshot.ConnectedClientsId.Add(1);
            m_RecvSnapshot.SendMessage = SendMessageRecvSide;
            m_RecvSnapshot.PreSpawnObject = PreSpawnObject;
            m_RecvSnapshot.PostSpawnObject = PostSpawnObject;
            m_RecvSnapshot.DespawnObject = DespawnObject;
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
            var targetClientIds = new List<ulong> { 1 };
            m_SendSnapshot.Spawn(command, null, targetClientIds);
        }

        private void SendDespawnToSnapshot(ulong objectId)
        {
            SnapshotDespawnCommand command = default;
            // identity
            command.NetworkObjectId = objectId;
            var targetClientIds = new List<ulong> { 1 };
            m_SendSnapshot.Despawn(command, null, targetClientIds);
        }

        [Test]
        public void TestSnapshotSequence()
        {
            int ticksToRun = 5;
            Prepare();

            m_SpawnedObjectCount = 0;
            m_NextSequence = 0;
            m_ExpectSpawns = false;
            m_ExpectDespawns = false;
            m_MinSpawns = 0;
            m_LoseNextMessage = true;
            m_PassBackResponses = false;

            for (int i = 0; i < ticksToRun; i++)
            {
                AdvanceOneTick();
            }
        }

        [Test]
        public void TestSnapshotSpawn()
        {
            Prepare();

            m_SpawnedObjectCount = 0;
            m_NextSequence = 0;
            m_ExpectSpawns = true;
            m_ExpectDespawns = false;
            m_MinSpawns = 2; // many spawns are to be sent together
            m_LoseNextMessage = false;
            m_PassBackResponses = false;

            var ticksToRun = 20;

            // spawns one more than current buffer size
            var objectsToSpawn = m_SendSnapshot.SpawnsBufferCount + 1;

            for (int i = 0; i < objectsToSpawn; i++)
            {
                SendSpawnToSnapshot((ulong)i);
            }

            for (int i = 0; i < ticksToRun; i++)
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

            // test that buffers actually shrink and will grow back to needed size
            m_SendSnapshot.ReduceBufferUsage();
            m_RecvSnapshot.ReduceBufferUsage();
            Debug.Assert(m_SendSnapshot.SpawnsBufferCount == 1);
            Debug.Assert(m_SendSnapshot.DespawnsBufferCount == 1);
            Debug.Assert(m_RecvSnapshot.SpawnsBufferCount == 1);
            Debug.Assert(m_RecvSnapshot.DespawnsBufferCount == 1);

            m_SpawnedObjectCount = 0;
            m_DespawnedObjectCount = 0;

            m_NextSequence = 0;
            m_ExpectSpawns = true;
            m_ExpectDespawns = false;
            m_MinDespawns = 2; // many despawns are to be sent together
            m_LoseNextMessage = false;
            m_PassBackResponses = false;

            var ticksToRun = 20;

            // spawns one more than current buffer size
            var objectsToSpawn = 10;

            for (int i = 0; i < objectsToSpawn; i++)
            {
                SendSpawnToSnapshot((ulong)i);
            }

            for (int i = 0; i < ticksToRun; i++)
            {
                AdvanceOneTick();
            }

            for (int i = 0; i < objectsToSpawn; i++)
            {
                SendDespawnToSnapshot((ulong)i);
            }

            m_ExpectSpawns = true; // the un'acked spawns will still be present
            m_MinSpawns = 1; // but we don't really care how they are grouped then
            m_ExpectDespawns = true;

            for (int i = 0; i < ticksToRun; i++)
            {
                AdvanceOneTick();
            }

            Debug.Assert(m_DespawnedObjectCount == objectsToSpawn);
        }

        [Test]
        public void TestSnapshotMessageLoss()
        {
            var r = new Random();
            Prepare();

            m_SpawnedObjectCount = 0;
            m_NextSequence = 0;
            m_ExpectSpawns = true;
            m_ExpectDespawns = false;
            m_MinSpawns = 1;
            m_LoseNextMessage = false;
            m_PassBackResponses = false;

            var ticksToRun = 10;

            for (int i = 0; i < ticksToRun; i++)
            {
                m_LoseNextMessage = (r.Next() % 2) > 0;

                SendSpawnToSnapshot((ulong)i);
                AdvanceOneTick();
            }

            m_LoseNextMessage = false;
            AdvanceOneTick();
            AdvanceOneTick();

            Debug.Assert(m_SpawnedObjectCount == ticksToRun);
        }

        [Test]
        public void TestSnapshotAcks()
        {
            Prepare();

            m_SpawnedObjectCount = 0;
            m_NextSequence = 0;
            m_ExpectSpawns = true;
            m_ExpectDespawns = false;
            m_MinSpawns = 1;
            m_LoseNextMessage = false;
            m_PassBackResponses = true;

            var objectsToSpawn = 10;

            for (int i = 0; i < objectsToSpawn; i++)
            {
                SendSpawnToSnapshot((ulong)i);
            }
            AdvanceOneTickSendSide(); // let's tick the send multiple time, to check it still tries to send
            AdvanceOneTickSendSide(); // let's tick the send multiple time, to check it still tries to send
            AdvanceOneTick();

            m_ExpectSpawns = false; // all spawns should have made it back and forth and be absent from next messages
            AdvanceOneTick();

            for (int i = 0; i < objectsToSpawn; i++)
            {
                SendDespawnToSnapshot((ulong)i);
            }

            m_ExpectDespawns = true; // we should now be seeing despawns
            AdvanceOneTickSendSide(); // let's tick the send multiple time, to check it still tries to send
            AdvanceOneTick();

            Debug.Assert(m_SpawnedObjectCount == objectsToSpawn);
        }
    }
}

