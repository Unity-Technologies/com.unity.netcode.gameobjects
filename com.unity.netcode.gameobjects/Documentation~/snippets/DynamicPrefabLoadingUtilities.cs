using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game
{
    /// <summary>
    /// A utilities class to handle the loading, tracking, and disposing of loaded network prefabs. Connection and
    /// disconnection payloads can be easily accessed from this class as well.
    /// </summary>
    /// <remarks>
    /// Artificial delay to the loading of a network prefab is disabled by default. To enable it, make sure to add
    /// ENABLE_ARTIFICIAL_DELAY as a scripting define symbol to your project's Player settings.
    /// </remarks>
    public static class DynamicPrefabLoadingUtilities
    {
        const int k_EmptyDynamicPrefabHash = -1;

        public static int HashOfDynamicPrefabGUIDs { get; private set; } = k_EmptyDynamicPrefabHash;

        static Dictionary<AddressableGUID, AsyncOperationHandle<GameObject>> s_LoadedDynamicPrefabResourceHandles = new Dictionary<AddressableGUID, AsyncOperationHandle<GameObject>>(new AddressableGUIDEqualityComparer());
        
        static List<AddressableGUID> s_DynamicPrefabGUIDs = new List<AddressableGUID>(); //cached list to avoid GC

        //A storage where we keep the association between the dynamic prefab (hash of it's GUID) and the clients that have it loaded
        static Dictionary<int, HashSet<ulong>> s_PrefabHashToClientIds = new Dictionary<int, HashSet<ulong>>();

        public static bool HasClientLoadedPrefab(ulong clientId, int prefabHash) => 
            s_PrefabHashToClientIds.TryGetValue(prefabHash, out var clientIds) && clientIds.Contains(clientId);

        public static bool IsPrefabLoadedOnAllClients(AddressableGUID assetGuid) => 
            s_LoadedDynamicPrefabResourceHandles.ContainsKey(assetGuid);

        public static bool TryGetLoadedGameObjectFromGuid(AddressableGUID assetGuid, out AsyncOperationHandle<GameObject> loadedGameObject)
        {
            return s_LoadedDynamicPrefabResourceHandles.TryGetValue(assetGuid, out loadedGameObject);
        }

        public static Dictionary<AddressableGUID, AsyncOperationHandle<GameObject>> LoadedDynamicPrefabResourceHandles => s_LoadedDynamicPrefabResourceHandles;

        public static int LoadedPrefabCount => s_LoadedDynamicPrefabResourceHandles.Count;

        static NetworkManager s_NetworkManager;

        static DynamicPrefabLoadingUtilities() { }

        public static void Init(NetworkManager networkManager)
        {
            s_NetworkManager = networkManager;
        }

        /// <remarks>
        /// This is not the most optimal algorithm for big quantities of Addressables, but easy enough to maintain for a
        /// small number like in this sample. One could use a "client dirty" algorithm to mark clients needing loading
        /// or not instead, but that would require more complex dirty management.
        /// </remarks>
        public static void RecordThatClientHasLoadedAllPrefabs(ulong clientId)
        {
            foreach (var dynamicPrefabGUID in s_DynamicPrefabGUIDs)
            {
                RecordThatClientHasLoadedAPrefab(dynamicPrefabGUID.GetHashCode(), clientId);
            }
        }
        
        public static void RecordThatClientHasLoadedAPrefab(int assetGuidHash, ulong clientId)
        {
            if (s_PrefabHashToClientIds.TryGetValue(assetGuidHash, out var clientIds))
            {
                clientIds.Add(clientId);
            }
            else
            {
                s_PrefabHashToClientIds.Add(assetGuidHash, new HashSet<ulong>() { clientId });
            }
        }

        public static byte[] GenerateRequestPayload()
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                hashOfDynamicPrefabGUIDs = HashOfDynamicPrefabGUIDs
            });

            return System.Text.Encoding.UTF8.GetBytes(payload);
        }

        /// <remarks>
        /// Testing showed that with the current implementation, Netcode for GameObjects will send the DisconnectReason
        /// message as a non-fragmented message, meaning that the upper limit of this message in bytes is exactly
        /// NON_FRAGMENTED_MESSAGE_MAX_SIZE bytes (1300 at the time of writing), defined inside of
        /// <see cref="MessagingSystem"/>.
        /// For this reason, DisconnectReason should only be used to instruct the user "why" a connection failed, and
        /// "where" to fetch the relevant connection data. We recommend using services like UGS to fetch larger batches
        /// of data.
        /// </remarks>
        public static string GenerateDisconnectionPayload()
        {
            var rejectionPayload = new DisconnectionPayload()
            {
                reason = DisconnectReason.ClientNeedsToPreload,
                guids = s_DynamicPrefabGUIDs.Select(item => item.ToString()).ToList()
            };
    
            return JsonUtility.ToJson(rejectionPayload);
        }
        
        public static async Task<GameObject> LoadDynamicPrefab(AddressableGUID guid, int artificialDelayMilliseconds, 
            bool recomputeHash = true)
        {
            if (s_LoadedDynamicPrefabResourceHandles.ContainsKey(guid))
            {
                Debug.Log($"Prefab has already been loaded, skipping loading this time | {guid}");
                return s_LoadedDynamicPrefabResourceHandles[guid].Result;
            }
            
            Debug.Log($"Loading dynamic prefab {guid.Value}");
            var op = Addressables.LoadAssetAsync<GameObject>(guid.ToString());
            var prefab = await op.Task;

#if ENABLE_ARTIFICIAL_DELAY
            // THIS IS FOR EDUCATIONAL PURPOSES AND SHOULDN'T BE INCLUDED IN YOUR PROJECT
            await Task.Delay(artificialDelayMilliseconds);
#endif

            s_NetworkManager.AddNetworkPrefab(prefab);
            s_LoadedDynamicPrefabResourceHandles.Add(guid, op);
            
            if (recomputeHash)
            {
                CalculateDynamicPrefabArrayHash();
            }

            return prefab;
        }
        
        public static async Task<IList<GameObject>> LoadDynamicPrefabs(AddressableGUID[] guids,
            int artificialDelaySeconds = 0)
        {
            var tasks = new List<Task<GameObject>>();

            foreach (var guid in guids)
            {
                tasks.Add( LoadDynamicPrefab(guid, artificialDelaySeconds, recomputeHash:false));
            }
            
            var prefabs = await Task.WhenAll(tasks);
            CalculateDynamicPrefabArrayHash();
            
            return prefabs;
        }

        public static void RefreshLoadedPrefabGuids()
        {
            s_DynamicPrefabGUIDs.Clear();
            s_DynamicPrefabGUIDs.AddRange(s_LoadedDynamicPrefabResourceHandles.Keys);
        }
        
        static void CalculateDynamicPrefabArrayHash()
        {
            //we need to sort the array so that the hash is consistent across clients
            //it's possible to use an order-independent hashing algorithm for some potential performance gains
            RefreshLoadedPrefabGuids();
            s_DynamicPrefabGUIDs.Sort((a, b) => a.Value.CompareTo(b.Value));
            HashOfDynamicPrefabGUIDs = k_EmptyDynamicPrefabHash;
            
            //a simple hash combination algorithm suggested by Jon Skeet,
            //found here: https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
            //we can't use C# HashCode combine because it is unreliable across different processes (by design)
            unchecked
            {
                int hash = 17;
                for (var i = 0; i < s_DynamicPrefabGUIDs.Count; ++i)
                {
                    hash = hash * 31 + s_DynamicPrefabGUIDs[i].GetHashCode();
                }

                HashOfDynamicPrefabGUIDs = hash;
            }

            Debug.Log($"Calculated hash of dynamic prefabs: {HashOfDynamicPrefabGUIDs}");
        }
        
        public static void UnloadAndReleaseAllDynamicPrefabs()
        {
            HashOfDynamicPrefabGUIDs = k_EmptyDynamicPrefabHash;
            
            foreach (var handle in s_LoadedDynamicPrefabResourceHandles.Values)
            {
                s_NetworkManager.RemoveNetworkPrefab(handle.Result);
                Addressables.Release(handle);
            }
            
            s_LoadedDynamicPrefabResourceHandles.Clear();
        }
    }
}
