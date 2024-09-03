using System.Collections.Generic;
using Unity.Multiplayer.Tools.Adapters.Utp2;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]
namespace Unity.Multiplayer.Tools.Adapters.Ngo1WithUtp2
{
    static class Ngo1WithUtp2AdapterInitializer
    {
        internal static readonly IDictionary<int, Utp2Adapter> s_Adapters = new Dictionary<int, Utp2Adapter>();

        [RuntimeInitializeOnLoadMethod]
        internal static void InitializeAdapter()
        {
            UnityTransport.TransportInitialized += AddAdapter;
            UnityTransport.TransportDisposed += RemoveAdapter;
        }

        static void AddAdapter(int instanceId, NetworkDriver networkDriver)
        {
            if (s_Adapters.ContainsKey(instanceId))
            {
                return;
            }

            var adapter = new Utp2Adapter(networkDriver);
            s_Adapters[instanceId] = adapter;
            NetworkAdapters.AddAdapter(adapter);
        }

        static void RemoveAdapter(int instanceId)
        {
            if (s_Adapters.TryGetValue(instanceId, out var adapter))
            {
                NetworkAdapters.RemoveAdapter(adapter);
                s_Adapters.Remove(instanceId);
            }
        }
    }
}
