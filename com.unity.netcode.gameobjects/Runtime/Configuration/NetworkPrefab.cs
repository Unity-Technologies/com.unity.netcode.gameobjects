using System;
using UnityEngine;

#if NETCODE_USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Unity.Netcode
{
    internal enum NetworkPrefabOverride
    {
        None,
        Prefab,
        Hash
    }

    /// <summary>
    /// Class that represents a NetworkPrefab
    /// </summary>
    [Serializable]
    internal class NetworkPrefab
    {
        /// <summary>
        /// The override setttings for this NetworkPrefab
        /// </summary>
        public NetworkPrefabOverride Override;

        /// <summary>
        /// Asset reference of the network prefab
        /// </summary>
        public GameObject Prefab;

        /// <summary>
        /// Used when prefab is selected for the source prefab to override value (i.e. direct reference, the prefab is within the same project)
        /// We keep a separate value as the user might want to have something different than the default Prefab for the SourcePrefabToOverride
        /// </summary>
        public GameObject SourcePrefabToOverride;

        /// <summary>
        /// Used when hash is selected for the source prefab to override value (i.e. a direct reference is not possible such as in a multi-project pattern)
        /// </summary>
        public uint SourceHashToOverride;

        /// <summary>
        /// The prefab to replace (override) the source prefab with
        /// </summary>
        public GameObject OverridingTargetPrefab;

        internal static void VerifyValidPrefab(GameObject prefab)
        {
            if (!prefab || !prefab.GetComponent<NetworkObject>())
            {
                throw new Exception($"{nameof(NetworkPrefab)} assets (and all children) MUST point to a GameObject with a {nameof(NetworkObject)} component.");
            }

            for (var i = 0; i < prefab.transform.childCount; ++i)
            {
                VerifyValidPrefab(prefab.transform.GetChild(i).gameObject);
            }
        }
    }

#if NETCODE_USE_ADDRESSABLES
    /// <summary>
    /// Class that represents a NetworkPrefab
    /// </summary>
    [Serializable]
    internal class NetworkAddressable
    {
        /// <summary>
        /// The override setttings for this NetworkPrefab
        /// </summary>
        public NetworkPrefabOverride Override;

        private GameObject m_Prefab;

        public GameObject Prefab
        {
            get
            {
                if (m_Prefab == null)
                {
                    throw new Exception("Not yet loaded.");
                }

                return m_Prefab;
            }
        }

        /// <summary>
        /// Asset reference of the network addressable
        /// </summary>
        public AssetReferenceGameObject Addressable;

        ~NetworkAddressable()
        {
            if (m_AsyncOperationHandle.IsValid())
            {
                Addressables.Release(m_AsyncOperationHandle);
            }
        }

        private AsyncOperationHandle<GameObject> m_AsyncOperationHandle;

        /// <summary>
        /// Used when prefab is selected for the source prefab to override value (i.e. direct reference, the prefab is within the same project)
        /// We keep a separate value as the user might want to have something different than the default Prefab for the SourcePrefabToOverride
        /// </summary>
        public AssetReferenceGameObject SourcePrefabToOverride;

        /// <summary>
        /// Used when hash is selected for the source prefab to override value (i.e. a direct reference is not possible such as in a multi-project pattern)
        /// </summary>
        public uint SourceHashToOverride;

        /// <summary>
        /// The prefab to replace (override) the source prefab with
        /// </summary>
        public AssetReferenceGameObject OverridingTargetAddressable;

        public bool ResolveAsync()
        {
            if (m_Prefab)
            {
                return true;
            }
            if (m_AsyncOperationHandle.IsValid())
            {
                if (m_AsyncOperationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var result = m_AsyncOperationHandle.Result;
                    NetworkPrefab.VerifyValidPrefab(result);
                    m_Prefab = result;
                    return true;
                }

                if (m_AsyncOperationHandle.Status == AsyncOperationStatus.Failed)
                {
                    throw new Exception($"Could not load addressable object: {Addressable.AssetGUID}");
                }

                return false;
            }

            if (Addressable.OperationHandle.IsValid())
            {
                m_AsyncOperationHandle = Addressable.OperationHandle.Convert<GameObject>();
                return false;
            }
            m_AsyncOperationHandle = Addressable.LoadAssetAsync();
            return false;
        }
    }
#endif
}
