using System;

namespace MLAPI.Data
{
    [Flags]
    public enum SecuritySendFlags
    {
        None = 0x0,
        Encrypted = 0x1,
        Authenticated = 0x2
    }
}