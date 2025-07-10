using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Preloading
{
    /// <summary>
    /// This is the simplest case of a dynamic prefab - we instruct all game instances to load a network prefab (it can
    /// be just one, it could also be a set of network prefabs) and inject them to NetworkManager's NetworkPrefabs list
    /// before starting the server. What's important is that it doesn't really matter where the prefab comes from. It
    /// could be a simple prefab or it could be an Addressable - it's all the same.
    /// </summary>
    /// <remarks>
    /// Here, we're serializing the AssetReferenceGameObject to this class, but ideally you'd want to authenticate
    /// players when your game starts up and have them fetch network prefabs from services such as UGS (see Remote
    /// Config). It should also be noted that this is a technique that could serve to decrease the install size of your
    /// application, since you'd be streaming in networked game assets dynamically.
    /// </remarks>
    public sealed class Preloading : MonoBehaviour
    {
        [SerializeField] AssetReferenceGameObject m_DynamicPrefabReference;
        
        [SerializeField] NetworkManager m_NetworkManager;
        
        async void Start()
        {
            await PreloadDynamicPlayerPrefab();
            //after we've waited for the prefabs to load - we can start the host or the client
        }

        // It's important to note that this isn't limited to PlayerPrefab, despite the method name you can add any
        // prefab to the list of prefabs that will be spawned.
        async Task PreloadDynamicPlayerPrefab()
        {
            Debug.Log($"Started to load addressable with GUID: {m_DynamicPrefabReference.AssetGUID}");
            var op =  Addressables.LoadAssetAsync<GameObject>(m_DynamicPrefabReference);
            var prefab = await op.Task;
            Addressables.Release(op);
            
            //it's important to actually add the player prefab to the list of network prefabs - it doesn't happen
            //automatically
            m_NetworkManager.AddNetworkPrefab(prefab);
            Debug.Log($"Loaded prefab has been assigned to NetworkManager's PlayerPrefab");
            
            // at this point we can easily change the PlayerPrefab
            m_NetworkManager.NetworkConfig.PlayerPrefab = prefab;
            
            // Forcing all game instances to load a set of network prefabs and having each game instance inject network
            // prefabs to NetworkManager's NetworkPrefabs list pre-connection time guarantees that all players will have
            // matching NetworkConfigs. This is why NetworkManager.ForceSamePrefabs is set to true. We let Netcode for
            // GameObjects validate the matching NetworkConfigs between clients and the server. If this is set to false
            // on the server, clients may join with a mismatching NetworkPrefabs list from the server. 
            m_NetworkManager.NetworkConfig.ForceSamePrefabs = true;
        }
    }
}
