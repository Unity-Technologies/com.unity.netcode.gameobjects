using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if NETCODE_USE_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Unity.Netcode.Addressables
{
    public class NetworkAddressablesPreloader : MonoBehaviour
    {
#if NETCODE_USE_ADDRESSABLES
        private readonly List<AsyncOperationHandle<GameObject>> m_PrefabHandles = new List<AsyncOperationHandle<GameObject>>();

        // Start is called before the first frame update
        private async Task Start()
        {
            // TODO: (Cosmin) Further investigate the pros/cons use of LoadAssetsAsync in this scenario to preload all of them at ones and cache on "big" handle rather than handles individually
            foreach (var networkAddressablesPrefab in NetworkManager.Singleton.NetworkConfig
                         .GetNetworkAddressablesPrefabs)
            {
                if (networkAddressablesPrefab?.Reference == null)
                {
                    continue;
                }

                Debug.LogFormat("Preloading addressable {0}", networkAddressablesPrefab.Reference.AssetGUID);
                var handle = networkAddressablesPrefab.Reference.LoadAssetAsync<GameObject>();
                await handle.Task;
                m_PrefabHandles.Add(handle);
            }
        }

        private void OnDestroy()
        {
            foreach (var assetHandle in m_PrefabHandles)
            {
                UnityEngine.AddressableAssets.Addressables.Release(assetHandle);
            }
        }
#endif
    }
}
