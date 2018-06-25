using System;

namespace MLAPI.Attributes
{
    /// <summary>
    /// The attribute to use for variables that should be automatically. replicated from Server to Client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SyncedVar : Attribute
    {
        /// <summary>
        /// The method name to invoke when the SyncVar get's updated.
        /// </summary>
        public string hookMethodName = string.Empty;
        /// <summary>
        /// If true, the syncedVar will only be synced to the owner.
        /// </summary>
        public bool target = false;
        /// <summary>
        /// The delay for syncing this variable.
        /// </summary>
        public float syncDelay = 0.1f;
        /// <summary>
        /// Gets the time the Syncedvar was last synced
        /// </summary>
        public float lastSyncTime { get; internal set; }
    }
}
