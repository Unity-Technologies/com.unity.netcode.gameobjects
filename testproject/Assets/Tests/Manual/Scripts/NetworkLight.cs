using Unity.Netcode;

namespace TestProject.ManualTests
{
    /// <summary>
    /// Only allows server to have the light enabled.
    /// Used for integration testing
    /// </summary>
    public class NetworkLight : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if(!IsServer)
            {
                gameObject.SetActive(false);
            }
            base.OnNetworkSpawn();
        }
    }
}
