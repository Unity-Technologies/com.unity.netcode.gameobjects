using Unity.Netcode.Interest;

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
