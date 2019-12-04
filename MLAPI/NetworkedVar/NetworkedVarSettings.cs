namespace MLAPI.NetworkedVar
{
    /// <summary>
    /// Delegate type for permission checking
    /// </summary>
    /// <param name="clientId">The clientId whose permissions to check</param>
    public delegate bool NetworkedVarPermissionsDelegate(ulong clientId);

    /// <summary>
    /// The settings class used by the build in NetworkVar implementations
    /// </summary>
    public class NetworkedVarSettings
    {
        /// <summary>
        /// Defines the write permissions for this var
        /// </summary>
        public NetworkedVarPermission WritePermission = NetworkedVarPermission.ServerOnly;
        /// <summary>
        /// Defines the read permissions for this var
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
        /// The maximum times per second this var will be synced.
        /// A value of 0 will cause the variable to sync as soon as possible after being changed.
        /// A value of less than 0 will cause the variable to sync only at once at spawn and not update again.
        /// </summary>
        public float SendTickrate = 0;
        /// <summary>
        /// The name of the channel to use for this variable.
        /// Variables with different channels will be split into different packets
        /// </summary>
        public string SendChannel = "MLAPI_DEFAULT_MESSAGE";

        /// <summary>
        /// Constructs a new NetworkedVarSettings instance
        /// </summary>
        public NetworkedVarSettings()
        {

        }
    }
}
