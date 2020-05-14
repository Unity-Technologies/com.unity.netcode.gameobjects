namespace MLAPI
{
    /// <summary>
    /// Represents an action when disconnected from a host
    /// </summary>
    public enum DisconnectAction
    {
        /// <summary>
        /// Shutdown mlapi
        /// </summary>
        Shutdown,
        /// <summary>
        /// Migrate to a new host
        /// </summary>
        Migrate,
        /// <summary>
        /// Become the new host
        /// </summary>
        Host
    }
}
