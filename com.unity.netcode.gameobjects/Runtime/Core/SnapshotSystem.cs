using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Netcode
{
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

        // snapshot internal
        internal int TickWritten;
        internal List<ulong> TargetClientIds;
        internal int TimesWritten;

        // for Metrics
        internal NetworkObject NetworkObject;
    }

    internal delegate int MockSendMessage(ref SnapshotDataMessage message, NetworkDelivery delivery, ulong clientId);
    internal delegate int MockSpawnObject(SnapshotSpawnCommand spawnCommand);
    internal delegate int MockDespawnObject(SnapshotDespawnCommand despawnCommand);

    internal class SnapshotSystem : INetworkUpdateSystem, IDisposable
    {
        private NetworkManager m_NetworkManager;
        private NetworkTickSystem m_NetworkTickSystem;

        // The tick we're currently processing (or last we processed, outside NetworkUpdate())
        private int m_CurrentTick = NetworkTickSystem.NoTick;

        // Settings
        internal bool IsServer { get; set; }
        internal bool IsConnectedClient { get; set; }
        internal ulong ServerClientId { get; set; }
        internal List<ulong> ConnectedClientsId { get; } = new List<ulong>();
        internal MockSendMessage MockSendMessage { get; set; }
        internal MockSpawnObject MockSpawnObject { get; set; }
        internal MockDespawnObject MockDespawnObject { get; set; }

        // Property showing visibility into inner workings, for testing
        internal int SpawnsBufferCount { get; private set; } = 100;
        internal int DespawnsBufferCount { get; private set; } = 100;

        internal SnapshotSystem(NetworkManager networkManager, NetworkConfig config, NetworkTickSystem networkTickSystem)
        {
            m_NetworkManager = networkManager;
            m_NetworkTickSystem = networkTickSystem;

            // register for updates in EarlyUpdate
            this.RegisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
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

        // where we build and send a snapshot to a given client
        private void SendSnapshot(ulong clientId)
        {

        }

        internal void Spawn(SnapshotSpawnCommand command)
        {
        }

        internal void Despawn(SnapshotDespawnCommand command)
        {
        }

        internal void Store(ulong networkObjectId, int behaviourIndex, int variableIndex, NetworkVariableBase networkVariable)
        {
        }

        internal void HandleSnapshot(ulong clientId, in SnapshotDataMessage message)
        {
        }

        // internal API to reduce buffer usage, where possible
        internal void ReduceBufferUsage()
        {

        }
    }
}
