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

        public override void OnEnabled()
        {
            // Use this instead of OnEnabled, that is because the OnEnable method is stolen by the NetworkedBehaviour class
            // Usage of OnEnable will throw a error pointing you to this method and WILL break the behaviour
        }

        public override void OnDisabled()
        {
            // Use this instead of OnDisable, that is because the OnDisable method is stolen by the NetworkedBehaviour class
            // Usage of OnDisable will throw a error pointing you to this method and WILL break the behaviour
        }

        public override void OnDestroyed()
        {
            // Use this instead of OnDestroy, that is because the OnDestroy method is stolen by the NetworkedBehaviour class
            // Usage of OnDestroy will throw a error pointing you to this method and WILL break the behaviour
        }
    }
}