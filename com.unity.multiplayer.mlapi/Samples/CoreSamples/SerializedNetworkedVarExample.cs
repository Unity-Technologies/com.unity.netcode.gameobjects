using MLAPI;
using MLAPI.NetworkVariable;

namespace MLAPI_Examples
{
    public class SerializedNetworkVariableExample : NetworkBehaviour
    {
        // Default settings, default value
        public NetworkVariableInt SerializedNetworkVariableInt = new NetworkVariableInt();
        // Default settings, initialization value 5
        public NetworkVariableInt SerializedNetworkVariableIntValue = new NetworkVariableInt(5);
        // Custom settings
        public NetworkVariableInt SerializedNetworkVariableIntSettings = new NetworkVariableInt(new NetworkVariableSettings()
        {
            SendChannel = "MySendChannel", // The var value will be synced over this channel
            ReadPermission = NetworkVariablePermission.Everyone, // The var values will be synced to everyone
            ReadPermissionCallback = null, // Only used when using "Custom" read permission
            SendTickrate = 2, // The var will sync no more than 2 times per second
            WritePermission = NetworkVariablePermission.OwnerOnly, // Only the owner of this object is allowed to change the value
            WritePermissionCallback = null // Only used when write permission is "Custom"
        });
    }
}