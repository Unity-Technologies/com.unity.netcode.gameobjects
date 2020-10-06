using MLAPI;
using MLAPI.NetworkedVar;

namespace MLAPI_Examples
{
    public class SerializedNetworkedVarExample : NetworkedBehaviour
    {
        // Default settings, default value
        public NetworkedVarInt SerializedNetworkedVarInt = new NetworkedVarInt();
        // Default settings, initialization value 5
        public NetworkedVarInt SerializedNetworkedVarIntValue = new NetworkedVarInt(5);
        // Custom settings
        public NetworkedVarInt SerializedNetworkedVarIntSettings = new NetworkedVarInt(new NetworkedVarSettings()
        {
            SendChannel = "MySendChannel", // The var value will be synced over this channel
            ReadPermission = NetworkedVarPermission.Everyone, // The var values will be synced to everyone
            ReadPermissionCallback = null, // Only used when using "Custom" read permission
            SendTickrate = 2, // The var will sync no more than 2 times per second
            WritePermission = NetworkedVarPermission.OwnerOnly, // Only the owner of this object is allowed to change the value
            WritePermissionCallback = null // Only used when write permission is "Custom"
        });
    }
}