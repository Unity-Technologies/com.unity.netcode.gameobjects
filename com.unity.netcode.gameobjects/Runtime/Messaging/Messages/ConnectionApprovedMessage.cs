using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Netcode.Messages
{
    internal struct ConnectionApprovedMessage: INetworkMessage
    {
        public ulong OwnerClientId;
        public int NetworkTick;
        public int SceneObjectCount;
        public NativeArray<NetworkObject.SceneObject> SceneObjects;

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
            if (SceneObjectCount != 0)
            {
                foreach (var sceneObject in SceneObjects)
                {
                    sceneObject.Serialize(ref writer);
                }

                SceneObjects.Dispose();
            }
        }

        public static void Receive(ref FastBufferReader reader, NetworkContext context)
        {
            if (!reader.TryBeginRead(sizeof(ulong) + sizeof(int) + sizeof(int)))
            {
                return;
            }

            var message = new ConnectionApprovedMessage();
            reader.ReadValue(out message.OwnerClientId);
            reader.ReadValue(out message.NetworkTick);
            reader.ReadValue(out message.SceneObjectCount);
            message.SceneObjects = new NativeArray<NetworkObject.SceneObject>(message.SceneObjectCount, Allocator.Temp);
            using (message.SceneObjects)
            {
                for (var i = 0; i < message.SceneObjectCount; ++i)
                {
                    message.SceneObjects[i] = new NetworkObject.SceneObject();
                    message.SceneObjects[i].Deserialize(ref reader);
                }
            }
            message.Handle(context.SenderId, (NetworkManager)context.SystemOwner);
        }

        public unsafe void Handle(ulong clientId, NetworkManager networkManager)
        {
            networkManager.LocalClientId = OwnerClientId;

            var time = new NetworkTime(networkManager.NetworkTickSystem.TickRate, NetworkTick);
            networkManager.NetworkTimeSystem.Reset(time.Time, 0.15f); // Start with a constant RTT of 150 until we receive values from the transport.

            networkManager.ConnectedClients.Add(networkManager.LocalClientId, new NetworkClient { ClientId = networkManager.LocalClientId });

            // Only if scene management is disabled do we handle NetworkObject synchronization at this point
            if (!networkManager.NetworkConfig.EnableSceneManagement)
            {
                networkManager.SpawnManager.DestroySceneObjects();

                NetworkObject.SceneObject* ptr = (NetworkObject.SceneObject*)SceneObjects.GetUnsafePtr();
                for (ushort i = 0; i < SceneObjectCount; i++)
                {
                    NetworkObject.AddSceneObject(ref ptr[i], networkManager);
                }

                // Mark the client being connected
                networkManager.IsConnectedClient = true;
                // When scene management is disabled we notify after everything is synchronized
                networkManager.InvokeOnClientConnectedCallback(clientId);
            }
        }
    }
}