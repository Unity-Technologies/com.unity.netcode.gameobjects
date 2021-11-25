using System.Collections.Generic;
using Unity.Netcode.Interest;
using UnityEngine;

namespace Unity.Netcode
{
    public class AddAllInterestKernel : IInterestKernel<NetworkObject>
    {

        public void QueryFor(NetworkObject clientNetworkObject, NetworkObject obj, HashSet<NetworkObject> results)
        {
            results.Add(obj.GetComponent<NetworkObject>());
        }
    }
}
