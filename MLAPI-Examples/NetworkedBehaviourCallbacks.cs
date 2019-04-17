using System.IO;
using MLAPI;

namespace MLAPI_Examples
{
    public class NetworkedBehaviourCallbacks : NetworkedBehaviour
    {
        public override void NetworkStart(Stream payload)
        {
            // Invoked when the object is spawned and ready
            // All properties like IsLocalPlayer etc is guaranteed to be ready
            // The NetWorkStart can be used with or without the "Stream payload" parameter
            // If it's included it will contain the payload included in the Spawn method if applicable.
        }
    }
}