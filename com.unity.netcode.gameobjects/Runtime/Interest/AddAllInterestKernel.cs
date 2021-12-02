using System.Collections.Generic;
using Unity.Netcode.Interest;
using UnityEngine;

namespace Unity.Netcode
{
    public class AddAllInterestKernel : IInterestKernel<NetworkObject>
    {

        public bool QueryFor(NetworkObject clientNetworkObject, NetworkObject obj)
        {
            return true;
        }
    }
}
