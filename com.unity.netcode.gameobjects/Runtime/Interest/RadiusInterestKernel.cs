using System;
using System.Collections.Generic;
using Unity.Netcode.Interest;
using UnityEngine;

namespace Unity.Netcode
{
    public class RadiusInterestKernel : IInterestKernel<NetworkObject>
    {
        public float Radius = 0.0f;

        public void QueryFor(NetworkObject clientNetworkObject, NetworkObject obj, HashSet<NetworkObject> results)
        {
            if (Vector3.Distance(obj.transform.position, clientNetworkObject.gameObject.transform.position) <= Radius)
            {
                results.Add(obj.GetComponent<NetworkObject>());
            }
        }
    }
}
