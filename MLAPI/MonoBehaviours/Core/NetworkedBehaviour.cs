using System;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.NetworkingManagerComponents;

namespace MLAPI
{
    public abstract class NetworkedBehaviour : MonoBehaviour
    {
        protected bool isLocalPlayer
        {
            get
            {
                return networkedObject.isLocalPlayer;
            }
        }
        protected bool isServer
        {
            get
            {
                return NetworkingManager.singleton.isServer;
            }
        }
        protected bool isClient
        {
            get
            {
                return NetworkingManager.singleton.isClient;
            }
        }
        protected bool isHost
        {
            get
            {
                return NetworkingManager.singleton.isHost;
            }
        }
        protected NetworkedObject networkedObject
        {
            get
            {
                if(_networkedObject == null)
                {
                    _networkedObject = GetComponentInParent<NetworkedObject>();
                }
                return _networkedObject;
            }
        }
        private NetworkedObject _networkedObject = null;
        public uint networkId
        {
            get
            {
                return networkedObject.NetworkId;
            }
        }

        public int ownerClientId
        {
            get
            {
                return networkedObject.OwnerClientId;
            }
        }

        //Change data type
        private Dictionary<string, int> registeredMessageHandlers = new Dictionary<string, int>();

        protected int RegisterMessageHandler(string name, Action<int, byte[]> action)
        {
            int counter = MessageManager.AddIncomingMessageHandler(name, action);
            registeredMessageHandlers.Add(name, counter);
            return counter;
        }

        protected void DeregisterMessageHandler(string name, int counter)
        {
            MessageManager.RemoveIncomingMessageHandler(name, counter);
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<string, int> pair in registeredMessageHandlers)
            {
                DeregisterMessageHandler(pair.Key, pair.Value);
            }
        }

        protected void SendToServer(string messageType, string channelName, byte[] data)
        {
            if (isServer)
            {
                MessageManager.InvokeMessageHandlers(messageType, data, -1);
            }
            else
            {
                NetworkingManager.singleton.Send(NetworkingManager.singleton.serverClientId, messageType, channelName, data);
            }
        }

        protected void SendToLocalClient(string messageType, string channelName, byte[] data)
        {
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(ownerClientId, messageType, channelName, data);
        }

        protected void SendToNonLocalClients(string messageType, string channelName, byte[] data)
        {
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(messageType, channelName, data, ownerClientId);
        }

        protected void SendToClient(int clientId, string messageType, string channelName, byte[] data)
        {
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(clientId, messageType, channelName, data);
        }

        protected void SendToClients(int[] clientIds, string messageType, string channelName, byte[] data)
        {
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(clientIds, messageType, channelName, data);
        }

        protected void SendToClients(List<int> clientIds, string messageType, string channelName, byte[] data)
        {
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(clientIds, messageType, channelName, data);
        }

        protected void SendToClients(string messageType, string channelName, byte[] data)
        {
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(messageType, channelName, data);
        }

        protected NetworkedObject GetNetworkedObject(uint networkId)
        {
            return SpawnManager.spawnedObjects[networkId];
        }
    }
}
