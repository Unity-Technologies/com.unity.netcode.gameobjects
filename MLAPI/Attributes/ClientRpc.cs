using System;

namespace MLAPI.Attributes
{
    /// <summary>
    /// This attribute is used to specify that this is a remote Client RPC
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRpc : Attribute
    {

    }
}
