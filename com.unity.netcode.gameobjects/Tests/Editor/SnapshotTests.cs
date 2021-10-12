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
        private SnapshotSystem m_SnapshotSystem;
        private NetworkTimeSystem m_TimeSystem;
        private NetworkTickSystem m_TickSystem;

        internal int SendMessage(in SnapshotDataMessage message, NetworkDelivery delivery, ulong clientId)
        {
            Debug.Log("Snapshot Message sent");

            Debug.Assert(message.Ack.LastReceivedSequence == 0);
            Debug.Assert(message.Ack.ReceivedSequenceMask == 0);
            Debug.Assert(message.Despawns.IsEmpty);
            Debug.Assert(message.Sequence == 0);
            Debug.Assert(message.Spawns.Length == 10);
            Debug.Assert(message.Entries.Length == 0);

            using FastBufferWriter writer = new FastBufferWriter(1024, Allocator.Temp);
            message.Serialize(writer);
            using FastBufferReader reader = new FastBufferReader(writer, Allocator.Temp);
            var context = new NetworkContext{SenderId = 0, Timestamp = 0.0f, SystemOwner = m_SnapshotSystem};
            SnapshotDataMessage.Receive(reader, context);

            m_SnapshotSystem.HandleSnapshot(0, message);

            return 0;
        }

        private void PrepareSendSideSnapshot()
        {
            var config = new NetworkConfig();

            config.UseSnapshotDelta = false;
            config.UseSnapshotSpawn = true;

            m_SnapshotSystem = new SnapshotSystem(null, config, m_TickSystem);

            m_SnapshotSystem.IsServer = true;
            m_SnapshotSystem.IsConnectedClient = false;
            m_SnapshotSystem.ServerClientId = 0;
            m_SnapshotSystem.ConnectedClientsId.Clear();
            m_SnapshotSystem.ConnectedClientsId.Add(1);
            m_SnapshotSystem.MockSendMessage = SendMessage;

        }

        void SendSpawnToSnapshot(ulong objectId)
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
            m_SnapshotSystem.Spawn(command);
        }

        [Test]
        public void TestSnapshot()
        {
            m_TickSystem = new NetworkTickSystem(15, 0.0, 0.0);
            m_TimeSystem = new NetworkTimeSystem(0.2, 0.2, 1.0);

            PrepareSendSideSnapshot();

            for (int i = 0; i < 10; i++)
            {
                SendSpawnToSnapshot((ulong)i);
            }

            m_TimeSystem.Advance(0.1);
            m_TickSystem.UpdateTick(m_TimeSystem.LocalTime, m_TimeSystem.ServerTime);
            m_SnapshotSystem.NetworkUpdate(NetworkUpdateStage.EarlyUpdate);
        }
    }
}

