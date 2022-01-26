using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.ResourceManagement.ResourceProviders;
#if NETCODE_USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Addressables = UnityEngine.AddressableAssets.Addressables;
#endif

public class AddressablesSpawner : MonoBehaviour
{
#if NETCODE_USE_ADDRESSABLES
    public List<AssetReferenceGameObject> NetworkedAddressables = new List<AssetReferenceGameObject>();
    public float Frequency = 1.0f;
    private int m_SpawnIndex = 0;

    private void OnServerStarted()
    {
        if (NetworkedAddressables.Count > 0)
        {
            StartCoroutine(SpawnRoutine());
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Frequency);
            // (Cosmin): the full reason of why we are accessing the AssetRef in the NetworkManager is described in the NetworkManager.cs, around line 1821.
            // The TL:DR is that we want to use the same AssetReferenceGameObject ref/pointer across the application, otherwise we wouldn't be able to
            // access the Asset object without calling LoadAssetAsync/Instantiate Async but that would come with some non-nengligible overhead +
            // some interesting bugs could pop up
            AssetReferenceGameObject reference = NetworkManager.LookupAddressableAssetReference(NetworkedAddressables[m_SpawnIndex].AssetGUID);

            Debug.Assert(reference != null); 
            Debug.Assert(reference.OperationHandle.Status == AsyncOperationStatus.Succeeded);
            Debug.LogFormat("Spawning addressable {0}", reference.RuntimeKey);

            var go = Instantiate((GameObject)reference.Asset, transform.position, Quaternion.identity, null);
            var networkObject = go.GetComponent<NetworkObject>();
            networkObject.Spawn();
            m_SpawnIndex = (m_SpawnIndex + 1) % NetworkedAddressables.Count;
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }

        StopAllCoroutines();
    }
#endif
}
