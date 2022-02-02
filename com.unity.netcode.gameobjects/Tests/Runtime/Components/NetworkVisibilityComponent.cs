#if INCLUDE_NETCODE_RUNTIME_TESTS
namespace Unity.Netcode.RuntimeTests
{
    public class NetworkVisibilityComponent : NetworkBehaviour
    {
        public void Hide()
        {
            GetComponent<NetworkObject>().CheckObjectVisibility += HandleCheckObjectVisibility;
        }

        protected virtual bool HandleCheckObjectVisibility(ulong clientId) => false;

    }
}
#endif
