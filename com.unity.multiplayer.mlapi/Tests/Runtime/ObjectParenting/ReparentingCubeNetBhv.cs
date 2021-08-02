namespace Unity.Netcode.RuntimeTests
{
    public class ReparentingCubeNetBhv : NetworkBehaviour
    {
        public NetworkObject ParentNetworkObject { get; private set; }

        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            ParentNetworkObject = parentNetworkObject;
        }
    }
}
