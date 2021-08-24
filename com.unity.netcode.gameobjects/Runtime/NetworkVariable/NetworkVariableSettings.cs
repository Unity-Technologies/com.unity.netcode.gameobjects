namespace Unity.Netcode
{
    /// <summary>
    /// Delegate type for permission checking
    /// </summary>
    /// <param name="clientId">The clientId whose permissions to check</param>
    public delegate bool NetworkVariablePermissionsDelegate(ulong clientId);

    /// <summary>
    /// The settings class used by the build in NetworkVar implementations
    /// </summary>
    public class NetworkVariableSettings
    {
        /// <summary>
        /// Defines the read permissions for this var
        /// </summary>
        public NetworkVariableReadPermission ReadPermission = NetworkVariableReadPermission.Everyone;
    }
}
