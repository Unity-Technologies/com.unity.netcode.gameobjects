using System.Threading.Tasks;
using Unity.Multiplayer.Tools.Common;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]
namespace Unity.Multiplayer.Tools.Adapters.Ngo1
{
    internal static class Ngo1AdapterInitializer
    {
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeAdapter()
        {
            InitializeAdapterAsync().Forget();
        }

        private static async Task InitializeAdapterAsync()
        {
            var networkManager = await GetNetworkManagerAsync();
            var ngo1Adapter = new Ngo1Adapter(networkManager);
            NetworkAdapters.AddAdapter(ngo1Adapter);
        }

        private static async Task<NetworkManager> GetNetworkManagerAsync()
        {
            while (NetworkManager.Singleton == null || NetworkManager.Singleton.NetworkTickSystem == null)
            {
                await Task.Yield();
            }

            return NetworkManager.Singleton;
        }
    }
}
