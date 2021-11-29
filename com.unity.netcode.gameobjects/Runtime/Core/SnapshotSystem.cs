using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct AckData
    {
        public ushort LastReceivedSequence;
        public ushort ReceivedSequenceMask;
    }

    internal struct SnapshotDespawnCommand
    {
        // identity
        internal ulong NetworkObjectId;

        // snapshot internal
        internal int TickWritten;
        internal List<ulong> TargetClientIds;
        internal int TimesWritten;

        // for Metrics
        internal NetworkObject NetworkObject;
    }

    internal struct SnapshotSpawnCommand
    {
        // identity
        internal ulong NetworkObjectId;

        // archetype
        internal uint GlobalObjectIdHash;
        internal bool IsSceneObject;

        // parameters
        internal bool IsPlayerObject;
        internal ulong OwnerClientId;
        internal ulong ParentNetworkId;
        internal Vector3 ObjectPosition;
        internal Quaternion ObjectRotation;
        internal Vector3 ObjectScale;
    }

    internal struct SnapshotSpawnCommandMeta
    {
        internal List<ulong> TargetClientIds;
    };

    /*
    // snapshot internal
        internal int TickWritten;
        internal List<ulong> TargetClientIds;
        internal int TimesWritten;

        // for Metrics
        internal NetworkObject NetworkObject;
    */

    internal delegate int SendMessageDelegate(ArraySegment<byte> message, ulong clientId);
    internal delegate int MockSpawnObject(SnapshotSpawnCommand spawnCommand);
    internal delegate int MockDespawnObject(SnapshotDespawnCommand despawnCommand);

    internal struct SnapshotHeader
    {
        internal int CurrentTick;
    }

    internal class SnapshotSystem : INetworkUpdateSystem, IDisposable
    {
        private NetworkManager m_NetworkManager;
        private NetworkTickSystem m_NetworkTickSystem;

        // The tick we're currently processing (or last we processed, outside NetworkUpdate())
        private int m_CurrentTick = NetworkTickSystem.NoTick;

        // The container for the spawn commands received by the user. This part can be written as-is to the message
        internal SnapshotSpawnCommand[] Spawns;
        // Information about Spawns. Entries are matched by index
        internal SnapshotSpawnCommandMeta[] SpawnsMeta;
        internal int NumSpawns = 0;

        internal SnapshotDespawnCommand[] Despawns;
        internal int NumDespawns = 0;

        private static int DebugNextId = 0;
        private int DebugMyId = 0;

        // Settings
        internal bool IsServer { get; set; }
        internal bool IsConnectedClient { get; set; }
        internal ulong ServerClientId { get; set; }
        internal List<ulong> ConnectedClientsId { get; } = new List<ulong>();
        internal SendMessageDelegate SendMessage { get; set; }
        internal MockSpawnObject MockSpawnObject { get; set; }
        internal MockDespawnObject MockDespawnObject { get; set; }

        // Property showing visibility into inner workings, for testing
        internal int SpawnsBufferCount { get; private set; } = 100;
        internal int DespawnsBufferCount { get; private set; } = 100;

        internal SnapshotSystem(NetworkManager networkManager, NetworkConfig config, NetworkTickSystem networkTickSystem)
        {
            m_NetworkManager = networkManager;
            m_NetworkTickSystem = networkTickSystem;

            // by default, let's send on the network. This can be overriden for tests
            SendMessage = NetworkSendMessage;

            // register for updates in EarlyUpdate
            this.RegisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);

            Spawns = new SnapshotSpawnCommand[SpawnsBufferCount];
            SpawnsMeta = new SnapshotSpawnCommandMeta[SpawnsBufferCount];
            Despawns = new SnapshotDespawnCommand[DespawnsBufferCount];

            DebugMyId = DebugNextId;
            DebugNextId++;
        }

        // returns the default client list: just the server, on clients, all clients, on the server
        internal List<ulong> GetClientList()
        {
            List<ulong> clientList;
            clientList = new List<ulong>();

            if (!IsServer)
            {
                clientList.Add(m_NetworkManager.ServerClientId);
            }
            else
            {
                foreach (var clientId in ConnectedClientsId)
                {
                    if (clientId != m_NetworkManager.ServerClientId)
                    {
                        clientList.Add(clientId);
                    }
                }
            }

            return clientList;
        }

        internal void UpdateClientServerData()
        {
        }

        internal ConnectionRtt GetConnectionRtt(ulong clientId)
        {
            return new ConnectionRtt();
        }

        public void Dispose()
        {
            this.UnregisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            if (updateStage == NetworkUpdateStage.EarlyUpdate)
            {
                UpdateClientServerData();

                var tick = m_NetworkTickSystem.LocalTime.Tick;

                if (tick != m_CurrentTick)
                {
                    m_CurrentTick = tick;
                    if (IsServer)
                    {
                        for (int i = 0; i < ConnectedClientsId.Count; i++)
                        {
                            var clientId = ConnectedClientsId[i];

                            // don't send to ourselves
                            if (clientId != ServerClientId)
                            {
                                SendSnapshot(clientId);
                            }
                        }
                    }
                    else if (IsConnectedClient)
                    {
                        SendSnapshot(ServerClientId);
                    }
                }
            }
        }

        private int UpperBoundSnapshotSize()
        {
            return 2000;
        }

        // where we build and send a snapshot to a given client
        private void SendSnapshot(ulong clientId)
        {
            var header = new SnapshotHeader();
            Debug.Log($"[{DebugMyId}] Sending snapshot {m_CurrentTick} to client {clientId}");

            header.CurrentTick = m_CurrentTick;
            using var snapshotSerializer = new FastBufferWriter(UpperBoundSnapshotSize(), Allocator.Temp);

            if (snapshotSerializer.TryBeginWrite(FastBufferWriter.GetWriteSize(header)))
            {
                snapshotSerializer.WriteValue(header);
            }

            SendMessage(snapshotSerializer.ToTempByteArray(), clientId);
        }

        internal void Spawn(SnapshotSpawnCommand command, NetworkObject networkObject, List<ulong> targetClientIds)
        {
            if (NumSpawns >= SpawnsBufferCount)
            {
                Array.Resize(ref Spawns, 2 * SpawnsBufferCount);
                Array.Resize(ref SpawnsMeta, 2 * SpawnsBufferCount);

                SpawnsBufferCount = SpawnsBufferCount * 2;
                // Debug.Log($"[JEFF] spawn size is now {m_MaxSpawns}");
            }

            if (NumSpawns < SpawnsBufferCount)
            {
                if (targetClientIds == default)
                {
                    targetClientIds = GetClientList();
                }

                // todo: store, for each client, the spawn not ack'ed yet,
                // to prevent sending despawns to them.
                // for clientData in client list
                // clientData.SpawnSet.Add(command.NetworkObjectId);

                // todo:
                // this 'if' might be temporary, but is needed to help in debugging
                // or maybe it stays
                if (targetClientIds.Count > 0)
                {
                    Spawns[NumSpawns] = command;
                    SpawnsMeta[NumSpawns].TargetClientIds = targetClientIds;
                    NumSpawns++;
                }

                if (m_NetworkManager)
                {
                    foreach (var dstClientId in targetClientIds)
                    {
                        m_NetworkManager.NetworkMetrics.TrackObjectSpawnSent(dstClientId, networkObject, 8);
                    }
                }
            }
        }

        internal void Despawn(SnapshotDespawnCommand command)
        {
        }

        internal void Store(ulong networkObjectId, int behaviourIndex, int variableIndex, NetworkVariableBase networkVariable)
        {
        }

        internal void HandleSnapshot(ulong clientId, ArraySegment<byte> message)
        {
            var header = new SnapshotHeader();

            var snapshotDeserializer = new FastBufferReader(message, Allocator.Temp, message.Count, message.Offset);
            if (snapshotDeserializer.TryBeginRead(FastBufferWriter.GetWriteSize(header)))
            {
                snapshotDeserializer.ReadValue(out header);
            }
            Debug.Log($"[{DebugMyId}] Got snapshot with CurrentTick {header.CurrentTick}");

        }

        // internal API to reduce buffer usage, where possible
        internal void ReduceBufferUsage()
        {

        }

        internal int NetworkSendMessage(ArraySegment<byte> message, ulong clientId)
        {
            m_NetworkManager.NetworkConfig.NetworkTransport.Send(m_NetworkManager.ClientIdToTransportId(clientId), message, NetworkDelivery.Unreliable);

            return 0;
        }
    }
}
