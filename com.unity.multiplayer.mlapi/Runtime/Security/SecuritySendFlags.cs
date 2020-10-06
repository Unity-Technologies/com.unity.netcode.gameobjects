using System;

namespace MLAPI.Security
{
    /// <summary>
    /// The security operations of a payload
    /// </summary>
    [Flags]
    public enum SecuritySendFlags
    {
        /// <summary>
        /// No security operations are applied
        /// </summary>
        None = 0x0,
        /// <summary>
        /// The payload is encrypted
        /// </summary>
        Encrypted = 0x1,
        /// <summary>
        /// The payload is authenticated
        /// </summary>
        Authenticated = 0x2
    }
}