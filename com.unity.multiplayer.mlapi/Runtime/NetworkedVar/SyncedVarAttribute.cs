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

        /// <summary>
        /// The maximum times per second this var will be synced.
        /// A value of 0 will cause the variable to sync as soon as possible after being changed.
        /// A value of less than 0 will cause the variable to sync only at once at spawn and not update again.
        /// </summary>
        public float SendTickrate = 0;
    }
}
