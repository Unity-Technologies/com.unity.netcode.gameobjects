using System;

namespace MLAPI.NetworkedVar
{
    /// <summary>
    /// SyncedVar attribute. Use this to automatically syncronize fields from the server to clients.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class SyncedVarAttribute : Attribute
    {
        /// <summary>
        /// The channel to send changes on.
        /// </summary>
        public string Channel = "MLAPI_DEFAULT_MESSAGE";
    }
}
