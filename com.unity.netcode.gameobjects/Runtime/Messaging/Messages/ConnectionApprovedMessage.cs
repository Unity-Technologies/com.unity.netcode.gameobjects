using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Netcode
{
    internal struct ConnectionApprovedMessage : INetworkMessage
    {
        private const int k_VersionAddClientIds = 1;
        public int Version => k_VersionAddClientIds;

        public ulong OwnerClientId;
        public int NetworkTick;

        // Not serialized, held as references to serialize NetworkVariable data
        public HashSet<NetworkObject> SpawnedObjectsList;

        private FastBufferReader m_ReceivedSceneObjectData;

        public NativeArray<MessageVersionData> MessageVersions;

        public NativeArray<ulong> ConnectedClientIds;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            // ============================================================
            // BEGIN FORBIDDEN SEGMENT
            // DO NOT CHANGE THIS HEADER. Everything added to this message
            // must go AFTER the message version header.
            // ============================================================
            BytePacker.WriteValueBitPacked(writer, MessageVersions.Length);
            foreach (var messageVersion in MessageVersions)
            {
                messageVersion.Serialize(writer);
            }
            // ============================================================
            // END FORBIDDEN SEGMENT
            // ============================================================

            BytePacker.WriteValueBitPacked(writer, OwnerClientId);
            BytePacker.WriteValueBitPacked(writer, NetworkTick);

            if (targetVersion >= k_VersionAddClientIds)
            {
                writer.WriteValueSafe(ConnectedClientIds);
            }

            uint sceneObjectCount = 0;

            // When SpawnedObjectsList is not null then scene management is disabled. Provide a list of
            // all observed and spawned NetworkObjects that the approved client needs to synchronize.
            if (SpawnedObjectsList != null)
            {
                var pos = writer.Position;
                writer.Seek(writer.Position + FastBufferWriter.GetWriteSize(sceneObjectCount));

                // Serialize NetworkVariable data
                foreach (var sobj in SpawnedObjectsList)
                {
                    if (sobj.SpawnWithObservers && (sobj.CheckObjectVisibility == null || sobj.CheckObjectVisibility(OwnerClientId)))
                    {
                        sobj.Observers.Add(OwnerClientId);
                        var sceneObject = sobj.GetMessageSceneObject(OwnerClientId);
                        sceneObject.Serialize(writer);
                        ++sceneObjectCount;
                    }
                }

                writer.Seek(pos);
                // Can't pack this value because its space is reserved, so it needs to always use all the reserved space.
                writer.WriteValueSafe(sceneObjectCount);
                writer.Seek(writer.Length);
            }
            else
            {
                writer.WriteValueSafe(sceneObjectCount);
            }
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }

            // ============================================================
            // BEGIN FORBIDDEN SEGMENT
            // DO NOT CHANGE THIS HEADER. Everything added to this message
            // must go AFTER the message version header.
            // ============================================================
            ByteUnpacker.ReadValueBitPacked(reader, out int length);
            var messageHashesInOrder = new NativeArray<uint>(length, Allocator.Temp);
            for (var i = 0; i < length; ++i)
            {
                var messageVersion = new MessageVersionData();
                messageVersion.Deserialize(reader);
                networkManager.ConnectionManager.MessageManager.SetVersion(context.SenderId, messageVersion.Hash, messageVersion.Version);
                messageHashesInOrder[i] = messageVersion.Hash;

                // Update the received version since this message will always be passed version 0, due to the map not
                // being initialized until just now.
                var messageType = networkManager.ConnectionManager.MessageManager.GetMessageForHash(messageVersion.Hash);
                if (messageType == typeof(ConnectionApprovedMessage))
                {
                    receivedMessageVersion = messageVersion.Version;
                }
            }
            networkManager.ConnectionManager.MessageManager.SetServerMessageOrder(messageHashesInOrder);
            messageHashesInOrder.Dispose();
            // ============================================================
            // END FORBIDDEN SEGMENT
            // ============================================================

            ByteUnpacker.ReadValueBitPacked(reader, out OwnerClientId);
            ByteUnpacker.ReadValueBitPacked(reader, out NetworkTick);

            if (receivedMessageVersion >= k_VersionAddClientIds)
            {
                reader.ReadValueSafe(out ConnectedClientIds, Allocator.TempJob);
            }
            else
            {
                ConnectedClientIds = new NativeArray<ulong>(0, Allocator.TempJob);
            }

            m_ReceivedSceneObjectData = reader;
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            networkManager.LocalClientId = OwnerClientId;
            networkManager.MessageManager.SetLocalClientId(networkManager.LocalClientId);
            networkManager.NetworkMetrics.SetConnectionId(networkManager.LocalClientId);

            var time = new NetworkTime(networkManager.NetworkTickSystem.TickRate, NetworkTick);
            networkManager.NetworkTimeSystem.Reset(time.Time, 0.15f); // Start with a constant RTT of 150 until we receive values from the transport.
            networkManager.NetworkTickSystem.Reset(networkManager.NetworkTimeSystem.LocalTime, networkManager.NetworkTimeSystem.ServerTime);

            networkManager.ConnectionManager.LocalClient.SetRole(false, true, networkManager);
            networkManager.ConnectionManager.LocalClient.IsApproved = true;
            networkManager.ConnectionManager.LocalClient.ClientId = OwnerClientId;
            // Stop the client-side approval timeout coroutine since we are approved.
            networkManager.ConnectionManager.StopClientApprovalCoroutine();

            networkManager.ConnectionManager.ConnectedClientIds.Clear();
            foreach (var clientId in ConnectedClientIds)
            {
                networkManager.ConnectionManager.ConnectedClientIds.Add(clientId);
            }

            // Only if scene management is disabled do we handle NetworkObject synchronization at this point
            if (!networkManager.NetworkConfig.EnableSceneManagement)
            {
                networkManager.SpawnManager.DestroySceneObjects();
                m_ReceivedSceneObjectData.ReadValueSafe(out uint sceneObjectCount);

                // Deserializing NetworkVariable data is deferred from Receive() to Handle to avoid needing
                // to create a list to hold the data. This is a breach of convention for performance reasons.
                for (ushort i = 0; i < sceneObjectCount; i++)
                {
                    var sceneObject = new NetworkObject.SceneObject();
                    sceneObject.Deserialize(m_ReceivedSceneObjectData);
                    NetworkObject.AddSceneObject(sceneObject, m_ReceivedSceneObjectData, networkManager);
                }

                // Mark the client being connected
                networkManager.IsConnectedClient = true;
                // When scene management is disabled we notify after everything is synchronized
                networkManager.ConnectionManager.InvokeOnClientConnectedCallback(context.SenderId);

                // For convenience, notify all NetworkBehaviours that synchronization is complete.
                foreach (var networkObject in networkManager.SpawnManager.SpawnedObjectsList)
                {
                    networkObject.InternalNetworkSessionSynchronized();
                }
            }

            ConnectedClientIds.Dispose();
        }
    }
}
