using MLAPI.Data.Transports;

namespace MLAPI.Data
{
    /// <summary>
    /// The settings class used by the build in NetworkVar implementations
    /// </summary>
    public class NetworkedVarSettings
    {
        /// <summary>
        /// Defines the read permissions for this var
        /// </summary>
        public NetworkedVarPermission WritePermission = NetworkedVarPermission.ServerOnly;
        /// <summary>
        /// Defines the write permissions for this var
        /// </summary>
        public NetworkedVarPermission ReadPermission = NetworkedVarPermission.Everyone;
        /// <summary>
        /// The delegate used to evaluate write permission when the "Custom" mode is used
        /// </summary>
        public NetworkedVarPermissionsDelegate WritePermissionCallback = null;
        /// <summary>
        /// The delegate used to evaluate read permission when the "Custom" mode is used
        /// </summary>
        public NetworkedVarPermissionsDelegate ReadPermissionCallback = null;
        /// <summary>
        /// If enabled, sends will be sent the as fast as possible. If disabled, the sendDelay will be used
        /// </summary>
        public bool SendOnChange = false;
        /// <summary>
        /// The minimum amount of delay in seconds between sends
        /// </summary>
        public float SendDelay = 0.1f;
        /// <summary>
        /// The name of the channel to use for this variable.
        /// Variables with different channels will be split into different packets
        /// </summary>
        public string SendChannel = "MLAPI_INTERNAL";
        
        /// <summary>
        /// Constructs a new NetworkedVarSettings instance
        /// </summary>
        public NetworkedVarSettings()
        {
            
        }
    }
    
    /// <summary>
    /// Delegate type for permission checking
    /// </summary>
    /// <param name="clientId">The clientId whose permissions to check</param>
    public delegate bool NetworkedVarPermissionsDelegate(uint clientId);

    /// <summary>
    /// Permission type
    /// </summary>
    public enum NetworkedVarPermission
    {
        /// <summary>
        /// Everyone
        /// </summary>
        Everyone,
        /// <summary>
        /// Server-only operation
        /// </summary>
        ServerOnly,
        /// <summary>
        /// Owner-ownly
        /// </summary>
        OwnerOnly,
        /// <summary>
        /// Custom delegate
        /// </summary>
        Custom
    }
}
