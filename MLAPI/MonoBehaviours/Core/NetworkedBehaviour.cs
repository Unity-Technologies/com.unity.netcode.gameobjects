using System;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.NetworkingManagerComponents;

namespace MLAPI
{
    public abstract class NetworkedBehaviour : MonoBehaviour
    {
        public bool isLocalPlayer
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
        protected bool isOwner
        {
            get
            {
                return networkedObject.isOwner;
            }
        }
        public NetworkedObject networkedObject
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

        private void OnEnable()
        {
            if (_networkedObject == null)
            {
                _networkedObject = GetComponentInParent<NetworkedObject>();
            }
        }

        internal bool networkedStartInvoked = false;
        public virtual void NetworkStart()
        {

        }

        public virtual void OnGainedOwnership()
        {

        }

        public virtual void OnLostOwnership()
        {

        }

        protected int RegisterMessageHandler(string name, Action<int, byte[]> action)
        {
            int counter = MessageManager.AddIncomingMessageHandler(name, action, networkId);
            registeredMessageHandlers.Add(name, counter);
            return counter;
        }

        protected void DeregisterMessageHandler(string name, int counter)
        {
            MessageManager.RemoveIncomingMessageHandler(name, counter, networkId);
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
            if(MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (isServer)
            {
                MessageManager.InvokeMessageHandlers(messageType, data, -1);
            }
            else
            {
                NetworkingManager.singleton.Send(NetworkingManager.singleton.serverClientId, messageType, channelName, data);
            }
        }

        protected void SendToServerTarget(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (isServer)
            {
                MessageManager.InvokeTargetedMessageHandler(messageType, data, -1, networkId);
            }
            else
            {
                NetworkingManager.singleton.Send(NetworkingManager.singleton.serverClientId, messageType, channelName, data, networkId);
            }
        }

        protected void SendToLocalClient(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageTypes.Contains(messageType)))
            {
                Debug.LogWarning("MLAPI: Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            NetworkingManager.singleton.Send(ownerClientId, messageType, channelName, data);
        }

        protected void SendToLocalClientTarget(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageTypes.Contains(messageType)))
            {
                Debug.LogWarning("MLAPI: Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            NetworkingManager.singleton.Send(ownerClientId, messageType, channelName, data, networkId);
        }

        protected void SendToNonLocalClients(string messageType, string channelName, byte[] data, bool ignoreHost = false)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(messageType, channelName, data, ownerClientId, null, ignoreHost);
        }

        protected void SendToNonLocalClientsTarget(string messageType, string channelName, byte[] data, bool ignoreHost = false)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(messageType, channelName, data, ownerClientId, networkId, true);
        }

        protected void SendToClient(int clientId, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageTypes.Contains(messageType)))
            {
                Debug.LogWarning("MLAPI: Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            NetworkingManager.singleton.Send(clientId, messageType, channelName, data);
        }

        protected void SendToClientTarget(int clientId, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer && (!NetworkingManager.singleton.NetworkConfig.AllowPassthroughMessages || !NetworkingManager.singleton.NetworkConfig.PassthroughMessageTypes.Contains(messageType)))
            {
                Debug.LogWarning("MLAPI: Invalid Passthrough send. Ensure AllowPassthroughMessages are turned on and that the MessageType " + messageType + " is registered as a passthroughMessageType");
                return;
            }
            NetworkingManager.singleton.Send(clientId, messageType, channelName, data, networkId);
        }

        protected void SendToClients(int[] clientIds, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(clientIds, messageType, channelName, data);
        }

        protected void SendToClientsTarget(int[] clientIds, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(clientIds, messageType, channelName, data, networkId);
        }

        protected void SendToClients(List<int> clientIds, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(clientIds, messageType, channelName, data);
        }

        protected void SendToClientsTarget(List<int> clientIds, string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(clientIds, messageType, channelName, data, networkId);
        }

        protected void SendToClients(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(messageType, channelName, data);
        }

        protected void SendToClientsTarget(string messageType, string channelName, byte[] data)
        {
            if (MessageManager.messageTypes[messageType] < 32)
            {
                Debug.LogWarning("MLAPI: Sending messages on the internal MLAPI channels is not allowed!");
                return;
            }
            if (!isServer)
            {
                Debug.LogWarning("MLAPI: Sending messages from client to other clients is not yet supported");
                return;
            }
            NetworkingManager.singleton.Send(messageType, channelName, data, networkId);
        }

        protected NetworkedObject GetNetworkedObject(uint networkId)
        {
            return SpawnManager.spawnedObjects[networkId];
        }
    }
}
