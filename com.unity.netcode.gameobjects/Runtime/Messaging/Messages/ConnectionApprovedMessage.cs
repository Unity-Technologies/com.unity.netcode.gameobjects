using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Netcode.Messages
{
    internal struct ConnectionApprovedMessage: INetworkMessage
    {
        public ulong OwnerClientId;
        public int NetworkTick;
        public int SceneObjectCount;
        
        // Not serialized, held as references to serialize NetworkVariable data
        public HashSet<NetworkObject> SpawnedObjectsList;

        public void Serialize(ref FastBufferWriter writer)
        {
            if (!writer.TryBeginWrite(sizeof(ulong) + sizeof(int) + sizeof(int)))
            {
                throw new OverflowException(
                    $"Not enough space in the write buffer to serialize {nameof(ConnectionApprovedMessage)}");
            }
            writer.WriteValue(OwnerClientId);
            writer.WriteValue(NetworkTick);
            writer.WriteValue(SceneObjectCount);
            
            if(SceneObjectCount != 0)
            {
                // Serialize NetworkVariable data
                foreach (var sobj in SpawnedObjectsList)
                {
                    if (sobj.CheckObjectVisibility == null || sobj.CheckObjectVisibility(OwnerClientId))
                    {
                        sobj.Observers.Add(OwnerClientId);
                        var sceneObject = sobj.GetMessageSceneObject(OwnerClientId);
                        sceneObject.Serialize(ref writer);
                    }
                }
            }
        }

        public static void Receive(ref FastBufferReader reader, NetworkContext context)
        {
            var networkManager = (NetworkManager) context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return;
            }
            
            if (!reader.TryBeginRead(sizeof(ulong) + sizeof(int) + sizeof(int)))
            {
                throw new OverflowException(
                    $"Not enough space in the buffer to read {nameof(ConnectionApprovedMessage)}");
            }

            var message = new ConnectionApprovedMessage();
            reader.ReadValue(out message.OwnerClientId);
            reader.ReadValue(out message.NetworkTick);
            reader.ReadValue(out message.SceneObjectCount);
            message.Handle(ref reader, context.SenderId, networkManager);
        }

        public void Handle(ref FastBufferReader reader, ulong clientId, NetworkManager networkManager)
        {
            networkManager.LocalClientId = OwnerClientId;

            var time = new NetworkTime(networkManager.NetworkTickSystem.TickRate, NetworkTick);
            networkManager.NetworkTimeSystem.Reset(time.Time, 0.15f); // Start with a constant RTT of 150 until we receive values from the transport.

            networkManager.ConnectedClients.Add(networkManager.LocalClientId, new NetworkClient { ClientId = networkManager.LocalClientId });

            // Only if scene management is disabled do we handle NetworkObject synchronization at this point
            if (!networkManager.NetworkConfig.EnableSceneManagement)
            {
                networkManager.SpawnManager.DestroySceneObjects();

                // Deserializing NetworkVariable data is deferred from Receive() to Handle to avoid needing
                // to create a list to hold the data. This is a breach of convention for performance reasons.
                for (ushort i = 0; i < SceneObjectCount; i++)
                {
                    var sceneObject = new NetworkObject.SceneObject();
                    sceneObject.Deserialize(ref reader);
                    NetworkObject.AddSceneObject(sceneObject, ref reader, networkManager);
                }

                // Mark the client being connected
                networkManager.IsConnectedClient = true;
                // When scene management is disabled we notify after everything is synchronized
                networkManager.InvokeOnClientConnectedCallback(clientId);
            }
        }
    }
}