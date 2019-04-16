using MLAPI;
using MLAPI.Messaging;

namespace MLAPI_Examples
{
    public class ConvenienceMessagingPermission : NetworkedBehaviour
    {
        [ServerRPC(RequireOwnership = false)]
        public void RunCodeOnServerWithoutOwnershipChecks(int randomNumber, int constantNumber)
        {
            // Both parameters are sent over the network
            // This method can be invoked by any client. If you need permissions verify the sender 
        }

        // Require ownership defaults to true
        [ServerRPC]
        public void RunCodeOnServerWithOwnershipChecks()
        {
            // Both parameters are sent over the network
            // This method will ONLY run when invoked by the owner of the object and is thus safe
        }
    }
}