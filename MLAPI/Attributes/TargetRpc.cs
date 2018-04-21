using System;

namespace MLAPI.Attributes
{
    /// <summary>
    /// This attribute is used to specify that this is a remote Target RPC
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TargetRpc : Attribute
    {

    }
}
