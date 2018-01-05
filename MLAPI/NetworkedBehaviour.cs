using System;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI
{
    public abstract class NetworkedBehaviour : MonoBehaviour
    {
        protected bool isLocalPlayer;
        protected bool isServer = NetworkingManager.singleton.isServer;

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
            foreach(KeyValuePair<string, int> pair in registeredMessageHandlers)
            {
                DeregisterMessageHandler(pair.Key, pair.Value);
            }
        }
    }
}
