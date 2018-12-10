using System.IO;

namespace MLAPI.NetworkedVar
{

    /// <summary>
    /// Interface for networked value containers
    /// </summary>
    public interface INetworkedVar
    {
        /// <summary>
        /// Returns the name of the channel to be used for syncing
        /// </summary>
        /// <returns>The name of the channel to be used for syncing</returns>
        string GetChannel();
        /// <summary>
        /// Resets the dirty state and marks the variable as synced / clean
        /// </summary>
        void ResetDirty();
        /// <summary>
        /// Gets wheter or not the container is dirty
        /// </summary>
        /// <returns>Wheter or not the container is dirty</returns>
        bool IsDirty();
        /// <summary>
        /// Gets wheter or not a specific client can write to the varaible
        /// </summary>
        /// <param name="clientId">The clientId of the remote client</param>
        /// <returns>Wheter or not the client can write to the variable</returns>
        bool CanClientWrite(uint clientId);
        /// <summary>
        /// Gets wheter or not a specific client can read to the varaible
        /// </summary>
        /// <param name="clientId">The clientId of the remote client</param>
        /// <returns>Wheter or not the client can read to the variable</returns>
        bool CanClientRead(uint clientId);
        /// <summary>
        /// Writes the dirty changes, that is, the changes since the variable was last dirty, to the writer
        /// </summary>
        /// <param name="stream">The stream to write the dirty changes to</param>
        void WriteDelta(Stream stream);
        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="stream">The stream to write the state to</param>
        void WriteField(Stream stream);
        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="stream">The stream to read the state from</param>
        void ReadField(Stream stream);
        /// <summary>
        /// Reads delta from the reader and applies them to the internal value
        /// </summary>
        /// <param name="stream">The stream to read the delta from</param>
        /// <param name="keepDirtyDelta">Whether or not the delta should be kept as dirty or consumed</param>
        void ReadDelta(Stream stream, bool keepDirtyDelta);
        /// <summary>
        /// Sets NetworkedBehaviour the container belongs to.
        /// </summary>
        /// <param name="behaviour">The behaviour the container behaves to</param>
        void SetNetworkedBehaviour(NetworkedBehaviour behaviour);
    }

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
        /// The maximum times per second this var will be synced.
        /// Less than or equal to 0 will cause the variable to sync as soon as possible after being changed.
        /// </summary>
        public float SendTickrate = 0;
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
