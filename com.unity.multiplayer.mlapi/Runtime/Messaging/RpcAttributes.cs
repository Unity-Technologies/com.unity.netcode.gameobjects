using System;

namespace MLAPI.Messaging
{
    /// <summary>
    /// <para>Represents the common base class for Rpc attributes.</para>
    /// </summary>
    public abstract class RpcAttribute : Attribute
    {
        public bool IsReliable = true;
    }

    /// <summary>
    /// <para>Marks a method as ServerRpc.</para>
    /// <para>A ServerRpc marked method will be fired by a client but executed on the server.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerRpcAttribute : RpcAttribute
    {
        /// <summary>
        /// Whether or not the ServerRpc should only be run if executed by the owner of the object
        /// </summary>
        public bool RequireOwnership = true;
    }

    /// <summary>
    /// <para>Marks a method as ClientRpc.</para>
    /// <para>A ClientRpc marked method will be fired by the server but executed on clients.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRpcAttribute : RpcAttribute
    {
    }
}
