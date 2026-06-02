#if COM_UNITY_MODULES_PHYSICS2D
using Unity.Netcode.Runtime;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// NetworkRigidbody allows for the use of <see cref="Rigidbody2D"/> on network objects. By controlling the kinematic
    /// mode of the rigidbody and disabling it on all peers but the authoritative one.
    /// </summary>
    [RequireComponent(typeof(NetworkTransform))]
    // TODO-UNIFIED: We should not require this for unified and come up with a different way of handling the dependency
#if !UNIFIED_NETCODE
    [RequireComponent(typeof(Rigidbody2D))]
#endif
    [AddComponentMenu("Netcode/Network Rigidbody 2D")]
    [HelpURL(HelpUrls.NetworkRigidbody2D)]
    public class NetworkRigidbody2D : NetworkRigidbodyBase
    {
        public Rigidbody2D Rigidbody2D => m_InternalRigidbody2D;
        protected virtual void Awake()
        {
            Initialize(RigidbodyTypes.Rigidbody2D);
        }
    }
}
#endif // COM_UNITY_MODULES_PHYSICS2D
