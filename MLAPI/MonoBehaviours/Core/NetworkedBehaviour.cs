using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
        protected bool isServer = NetworkingManager.singleton.isServer;
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
        protected uint netId
        {
            get
            {
                return networkedObject.NetworkId;
            }
        }

        //Change data type
        private Dictionary<string, int> registeredMessageHandlers = new Dictionary<string, int>();

        public int RegisterMessageHandler(string name, Action<int, byte[]> action)
        {
            int counter = NetworkingManager.singleton.AddIncomingMessageHandler(name, action);
            registeredMessageHandlers.Add(name, counter);
            return counter;
        }

        public void DeregisterMessageHandler(string name, int counter)
        {
            NetworkingManager.singleton.RemoveIncomingMessageHandler(name, counter);
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<string, int> pair in registeredMessageHandlers)
            {
                DeregisterMessageHandler(pair.Key, pair.Value);
            }
        }

        public void Send(int connectionId, string messageType, string channelName, byte[] data)
        {
            Send(connectionId, messageType, channelName, data);
        }

        public void Send(int[] connectonIds, string messageType, string channelName, byte[] data)
        {
            for (int i = 0; i < connectonIds.Length; i++)
            {
                Send(connectonIds[i], messageType, channelName, data);
            }
        }

        public void Send(List<int> connectonIds, string messageType, string channelName, byte[] data)
        {
            for (int i = 0; i < connectonIds.Count; i++)
            {
                Send(connectonIds[i], messageType, channelName, data);
            }
        }
    }
}
