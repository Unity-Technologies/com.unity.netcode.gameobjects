using System;
using UnityEngine;

#if NETCODE_USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
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

#if NETCODE_USE_ADDRESSABLES
        [NonSerialized]
        public bool IsFromAddressable;

        [NonSerialized]
        public AssetReferenceGameObject Addressable;
#endif

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
}
