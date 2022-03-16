#if NETCODE_USE_ADDRESSABLES
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Unity.Netcode
{
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

        public void Release()
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
                Addressables.ResourceManager.Acquire(m_AsyncOperationHandle);
                return false;
            }
            m_AsyncOperationHandle = Addressable.LoadAssetAsync();
            return false;
        }
    }
}
#endif
