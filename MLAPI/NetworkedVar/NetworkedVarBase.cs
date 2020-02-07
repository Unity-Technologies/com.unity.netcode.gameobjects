using System.IO;

namespace MLAPI.NetworkedVar
{
    /// <summary>
    /// Abstract base class for networked value containers
    /// </summary>
    public abstract class NetworkedVarBase
    {
        /// <summary>
        /// The NetworkedBehaviour the container belongs to.
        /// </summary>
        public NetworkedBehaviour networkedBehaviour
        {
            get; internal set;
        }
        /// <summary>
        /// Returns the name of the channel to be used for syncing
        /// </summary>
        /// <returns>The name of the channel to be used for syncing</returns>
        public abstract string GetChannel();
        /// <summary>
        /// Resets the dirty state and marks the variable as synced / clean
        /// </summary>
        public abstract void ResetDirty();
        /// <summary>
        /// Gets Whether or not the container is dirty
        /// </summary>
        /// <returns>Whether or not the container is dirty</returns>
        public abstract bool IsDirty();
        /// <summary>
        /// Gets Whether or not a specific client can write to the varaible
        /// </summary>
        /// <param name="clientId">The clientId of the remote client</param>
        /// <returns>Whether or not the client can write to the variable</returns>
        public abstract bool CanClientWrite(ulong clientId);
        /// <summary>
        /// Gets Whether or not a specific client can read to the varaible
        /// </summary>
        /// <param name="clientId">The clientId of the remote client</param>
        /// <returns>Whether or not the client can read to the variable</returns>
        public abstract bool CanClientRead(ulong clientId);
        /// <summary>
        /// Writes the dirty changes, that is, the changes since the variable was last dirty, to the writer
        /// </summary>
        /// <param name="stream">The stream to write the dirty changes to</param>
        public abstract void WriteDelta(Stream stream);
        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="stream">The stream to write the state to</param>
        public abstract void WriteField(Stream stream);
        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="stream">The stream to read the state from</param>
        public abstract void ReadField(Stream stream);
        /// <summary>
        /// Reads delta from the reader and applies them to the internal value
        /// </summary>
        /// <param name="stream">The stream to read the delta from</param>
        /// <param name="keepDirtyDelta">Whether or not the delta should be kept as dirty or consumed</param>
        public abstract void ReadDelta(Stream stream, bool keepDirtyDelta);
    }
}
