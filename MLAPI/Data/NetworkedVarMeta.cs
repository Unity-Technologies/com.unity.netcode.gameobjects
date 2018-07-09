using MLAPI.Data.Transports;

namespace MLAPI.Data
{
    public class NetworkedVarSettings
    {
        public NetworkedVarPermission WritePermission = NetworkedVarPermission.ServerOnly;
        public NetworkedVarPermission ReadPermission = NetworkedVarPermission.Everyone;
        public NetworkedVarPermissionsDelegate WritePermissionCallback = null;
        public NetworkedVarPermissionsDelegate ReadPermissionCallback = null;
        public bool SendOnChange = false;
        public float SendDelay = 0.1f;
        public string SendChannel = "MLAPI_INTERNAL";
        
        public NetworkedVarSettings()
        {
            
        }
    }
    
    public delegate bool NetworkedVarPermissionsDelegate(uint clientId);

    public enum NetworkedVarPermission
    {
        Everyone,
        ServerOnly,
        OwnerOnly,
        Custom
    }
}
