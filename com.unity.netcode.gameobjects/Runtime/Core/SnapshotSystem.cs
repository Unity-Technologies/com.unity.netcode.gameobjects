using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct SnapshotHeader
    {
        internal int CurrentTick;
        internal ushort LastReceivedSequence;
        internal ushort ReceivedSequenceMask;

    }

    internal struct SnapshotDespawnCommand
    {
        // identity
        internal ulong NetworkObjectId;

        // snapshot internal
        internal int TickWritten;
    }

    internal struct SnapshotDespawnCommandMeta
    {
        internal List<ulong> TargetClientIds;
    };

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

        // internal
        internal int TickWritten;
    }

    internal struct SnapshotSpawnCommandMeta
    {
        internal List<ulong> TargetClientIds;
    };

    internal struct SentSpawnDespawn
    {
        internal ulong ObjectId;
        internal int Tick;
    }

    internal struct ClientData
    {
        internal ushort LastReceivedTick; // the last tick received by this client

        // by objectId
        // which spawns and despawns did this connection ack'ed ?
        internal Dictionary<ulong, int> SpawnDespawnAck;// = new Dictionary<ulong, int>();

        // list of spawn and despawns commands we sent, with sequence number
        // need to manage acknowledgements
        internal List<SentSpawnDespawn> SentSpawns;// = new List<SentSpawnDespawn>();
    }

    internal delegate int SendMessageDelegate(SnapshotDataMessage message, ulong clientId);
    internal delegate void SpawnObjectDelegate(SnapshotSpawnCommand spawnCommand, ulong srcClientId);
    internal delegate void DespawnObjectDelegate(SnapshotDespawnCommand despawnCommand, ulong srcClientId);

    internal class SnapshotSystem : INetworkUpdateSystem, IDisposable
    {
        private NetworkManager m_NetworkManager;
        private NetworkTickSystem m_NetworkTickSystem;

        private Dictionary<ulong, ClientData> m_ClientData = new Dictionary<ulong, ClientData>();
        private Dictionary<ulong, ConnectionRtt> m_ConnectionRtts = new Dictionary<ulong, ConnectionRtt>();

        // The tick we're currently processing (or last we processed, outside NetworkUpdate())
        private int m_CurrentTick = NetworkTickSystem.NoTick;

        // The container for the spawn commands received by the user. This part can be written as-is to the message
        internal SnapshotSpawnCommand[] Spawns;
        // Information about Spawns. Entries are matched by index
        internal SnapshotSpawnCommandMeta[] SpawnsMeta;
        internal int NumSpawns = 0;

        // The container for the despawn commands received by the user. This part can be written as-is to the message
        internal SnapshotDespawnCommand[] Despawns;
        // Information about Despawns. Entries are matched by index
        internal SnapshotDespawnCommandMeta[] DespawnsMeta;
        internal int NumDespawns = 0;

        private static int s_DebugNextId = 0;
        private int m_DebugMyId = 0;

        // Local state. Stores which spawns and despawns were applied locally
        // indexed by ObjectId
        internal Dictionary<ulong, int> TickAppliedSpawn = new Dictionary<ulong, int>();
        internal Dictionary<ulong, int> TickAppliedDespawn = new Dictionary<ulong, int>();

        // Settings
        internal bool IsServer { get; set; }
        internal bool IsConnectedClient { get; set; }
        internal ulong ServerClientId { get; set; }
        internal List<ulong> ConnectedClientsId { get; } = new List<ulong>();
        internal SendMessageDelegate SendMessage { get; set; }
        internal SpawnObjectDelegate SpawnObject { get; set; }
        internal DespawnObjectDelegate DespawnObject { get; set; }

        // Property showing visibility into inner workings, for testing
        internal int SpawnsBufferCount { get; private set; } = 100;
        internal int DespawnsBufferCount { get; private set; } = 100;

        internal SnapshotSystem(NetworkManager networkManager, NetworkConfig config, NetworkTickSystem networkTickSystem)
        {
            m_NetworkManager = networkManager;
            m_NetworkTickSystem = networkTickSystem;

            // by default, let's send on the network. This can be overriden for tests
            SendMessage = NetworkSendMessage;
            // by default, let's spawn with the rest of our package. This can be overriden for tests
            SpawnObject = NetworkSpawnObject;
            DespawnObject = NetworkDespawnObject;

            // register for updates in EarlyUpdate
            this.RegisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);

            Spawns = new SnapshotSpawnCommand[SpawnsBufferCount];
            SpawnsMeta = new SnapshotSpawnCommandMeta[SpawnsBufferCount];
            Despawns = new SnapshotDespawnCommand[DespawnsBufferCount];
            DespawnsMeta = new SnapshotDespawnCommandMeta[DespawnsBufferCount];

            m_DebugMyId = s_DebugNextId;
            s_DebugNextId++;
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

        // internal API to reduce buffer usage, where possible
        internal void ReduceBufferUsage()
        {
            var count = Math.Max(1, NumDespawns);
            Array.Resize(ref Despawns, count);
            DespawnsBufferCount = count;

            count = Math.Max(1, NumSpawns);
            Array.Resize(ref Spawns, count);
            SpawnsBufferCount = count;
        }

        internal void NetworkSpawnObject(SnapshotSpawnCommand spawnCommand, ulong srcClientId)
        {
            NetworkObject networkObject;
            if (spawnCommand.ParentNetworkId == spawnCommand.NetworkObjectId)
            {
                networkObject = m_NetworkManager.SpawnManager.CreateLocalNetworkObject(false,
                    spawnCommand.GlobalObjectIdHash, spawnCommand.OwnerClientId, null, spawnCommand.ObjectPosition,
                    spawnCommand.ObjectRotation);
            }
            else
            {
                networkObject = m_NetworkManager.SpawnManager.CreateLocalNetworkObject(false,
                    spawnCommand.GlobalObjectIdHash, spawnCommand.OwnerClientId, spawnCommand.ParentNetworkId, spawnCommand.ObjectPosition,
                    spawnCommand.ObjectRotation);
            }

            m_NetworkManager.SpawnManager.SpawnNetworkObjectLocally(networkObject, spawnCommand.NetworkObjectId,
                true, spawnCommand.IsPlayerObject, spawnCommand.OwnerClientId, false);
            //todo: discuss with tools how to report shared bytes
            m_NetworkManager.NetworkMetrics.TrackObjectSpawnReceived(srcClientId, networkObject, 8);
        }

        internal void NetworkDespawnObject(SnapshotDespawnCommand despawnCommand, ulong srcClientId)
        {
            m_NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(despawnCommand.NetworkObjectId, out NetworkObject networkObject);

            m_NetworkManager.SpawnManager.OnDespawnObject(networkObject, true);
            //todo: discuss with tools how to report shared bytes
            m_NetworkManager.NetworkMetrics.TrackObjectDestroyReceived(srcClientId, networkObject, 8);
        }

        internal void UpdateClientServerData()
        {
            if (m_NetworkManager)
            {
                IsServer = m_NetworkManager.IsServer;
                IsConnectedClient = m_NetworkManager.IsConnectedClient;
                ServerClientId = m_NetworkManager.ServerClientId;

                // todo: This is extremely inefficient. What is the efficient and idiomatic way ?
                ConnectedClientsId.Clear();
                if (IsServer)
                {
                    foreach (var id in m_NetworkManager.ConnectedClientsIds)
                    {
                        ConnectedClientsId.Add(id);
                    }
                }
            }
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
            return 10000;
        }

        // where we build and send a snapshot to a given client
        private void SendSnapshot(ulong clientId)
        {
            var header = new SnapshotHeader();
            var message = new SnapshotDataMessage(0);

            header.CurrentTick = m_CurrentTick;

            if (message.WriteBuffer.TryBeginWrite(FastBufferWriter.GetWriteSize(header)))
            {
                message.WriteBuffer.WriteValue(header);
            }

            // Find which spawns must be included
            var spawnsToInclude = new List<int>();
            for (var index = 0; index < NumSpawns; index++)
            {
                if (SpawnsMeta[index].TargetClientIds.Contains(clientId))
                {
                    spawnsToInclude.Add(index);
                }
            }

            // Write the Spawns. Count first, then each spawn
            if (message.WriteBuffer.TryBeginWrite(FastBufferWriter.GetWriteSize(spawnsToInclude.Count) +
                                                 spawnsToInclude.Count * FastBufferWriter.GetWriteSize(Spawns[0])))
            {
                message.WriteBuffer.WriteValue(spawnsToInclude.Count);
                foreach (var index in spawnsToInclude)
                {
                    message.WriteBuffer.WriteValue(Spawns[index]);
                }
            }

            // Find which despawns must be included
            var despawnsToInclude = new List<int>();
            for (var index = 0; index < NumDespawns; index++)
            {
                if (DespawnsMeta[index].TargetClientIds.Contains(clientId))
                {
                    despawnsToInclude.Add(index);
                }
            }

            // Write the Despawns. Count first, then each despawn
            if (message.WriteBuffer.TryBeginWrite(FastBufferWriter.GetWriteSize(despawnsToInclude.Count) +
                                                  despawnsToInclude.Count * FastBufferWriter.GetWriteSize(Despawns[0])))
            {
                message.WriteBuffer.WriteValue(despawnsToInclude.Count);
                foreach (var index in despawnsToInclude)
                {
                    message.WriteBuffer.WriteValue(Despawns[index]);
                }
            }

            SendMessage(message, clientId);
        }

        internal void Spawn(SnapshotSpawnCommand command, NetworkObject networkObject, List<ulong> targetClientIds)
        {
            command.TickWritten = m_CurrentTick;

            if (NumSpawns >= SpawnsBufferCount)
            {
                SpawnsBufferCount = SpawnsBufferCount * 2;
                Array.Resize(ref Spawns, SpawnsBufferCount);
                Array.Resize(ref SpawnsMeta, SpawnsBufferCount);
            }

            if (targetClientIds == default)
            {
                targetClientIds = GetClientList();
            }

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

        internal void Despawn(SnapshotDespawnCommand command, NetworkObject networkObject, List<ulong> targetClientIds)
        {
            command.TickWritten = m_CurrentTick;

            if (NumDespawns >= DespawnsBufferCount)
            {
                DespawnsBufferCount = DespawnsBufferCount * 2;
                Array.Resize(ref Despawns, DespawnsBufferCount);
                Array.Resize(ref DespawnsMeta, DespawnsBufferCount);
            }

            if (targetClientIds == default)
            {
                targetClientIds = GetClientList();
            }

            // todo:
            // this 'if' might be temporary, but is needed to help in debugging
            // or maybe it stays
            if (targetClientIds.Count > 0)
            {
                Despawns[NumDespawns] = command;
                DespawnsMeta[NumDespawns].TargetClientIds = targetClientIds;
                NumDespawns++;
            }

            if (m_NetworkManager)
            {
                foreach (var dstClientId in targetClientIds)
                {
                    m_NetworkManager.NetworkMetrics.TrackObjectDestroySent(dstClientId, networkObject, 8);
                }
            }
        }

        internal void Store(ulong networkObjectId, int behaviourIndex, int variableIndex, NetworkVariableBase networkVariable)
        {
        }

        internal void HandleSnapshot(ulong clientId, SnapshotDataMessage message)
        {
            // Read the Spawns. Count first, then each spawn
            var spawnCommand = new SnapshotSpawnCommand();
            var despawnCommand = new SnapshotDespawnCommand();

            var header = new SnapshotHeader();

            if (message.ReadBuffer.TryBeginRead(FastBufferWriter.GetWriteSize(header)))
            {
                message.ReadBuffer.ReadValue(out header);
            }

            var spawnCount = 0;
            if (message.ReadBuffer.TryBeginRead(FastBufferWriter.GetWriteSize(spawnCount)))
            {
                message.ReadBuffer.ReadValue(out spawnCount);
                if (!message.ReadBuffer.TryBeginRead(FastBufferWriter.GetWriteSize(spawnCommand) * spawnCount))
                {
                    // todo: deal with error
                }

                for (int index = 0; index < spawnCount; index++)
                {
                    message.ReadBuffer.ReadValue(out spawnCommand);

                    if (TickAppliedSpawn.ContainsKey(spawnCommand.NetworkObjectId) &&
                        spawnCommand.TickWritten <= TickAppliedSpawn[spawnCommand.NetworkObjectId])
                    {
                        continue;
                    }

                    TickAppliedSpawn[spawnCommand.NetworkObjectId] = spawnCommand.TickWritten;
                    SpawnObject(spawnCommand, clientId);
                }
            }

            var despawnCount = 0;
            if (message.ReadBuffer.TryBeginRead(FastBufferWriter.GetWriteSize(despawnCount)))
            {
                message.ReadBuffer.ReadValue(out despawnCount);
                if (!message.ReadBuffer.TryBeginRead(FastBufferWriter.GetWriteSize(despawnCommand) * despawnCount))
                {
                    // todo: deal with error
                }
                for (int index = 0; index < despawnCount; index++)
                {
                    message.ReadBuffer.ReadValue(out despawnCommand);

                    if (TickAppliedDespawn.ContainsKey(despawnCommand.NetworkObjectId) &&
                        despawnCommand.TickWritten <= TickAppliedDespawn[despawnCommand.NetworkObjectId])
                    {
                        continue;
                    }

                    TickAppliedDespawn[despawnCommand.NetworkObjectId] = despawnCommand.TickWritten;
                    DespawnObject(despawnCommand, clientId);
                }
            }
        }

        internal int NetworkSendMessage(SnapshotDataMessage message, ulong clientId)
        {
            m_NetworkManager.SendMessage(ref message, NetworkDelivery.Unreliable, clientId);

            return 0;
        }
    }
}
