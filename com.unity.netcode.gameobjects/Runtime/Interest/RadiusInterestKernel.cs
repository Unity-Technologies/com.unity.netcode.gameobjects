using Unity.Netcode.Interest;
using UnityEngine;

namespace Unity.Netcode
{
    public class RadiusInterestKernel : IInterestKernel<NetworkObject>
    {
        public float Radius = 0.0f;

        public bool QueryFor(NetworkObject clientNetworkObject, NetworkObject obj)
        {
            return Vector3.Distance(obj.transform.position, clientNetworkObject.gameObject.transform.position) <= Radius;
        }
    }
}
