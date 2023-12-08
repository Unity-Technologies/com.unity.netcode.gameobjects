using System;

namespace Unity.Netcode
{
    /// <summary>
    /// RPC delivery types
    /// </summary>
    public enum RpcDelivery
    {
        /// <summary>
        /// Reliable delivery
        /// </summary>
        Reliable = 0,

        /// <summary>
        /// Unreliable delivery
        /// </summary>
        Unreliable
    }

    /// <summary>
    /// <para>Represents the common base class for Rpc attributes.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RpcAttribute : Attribute
    {
        // Must match the set of parameters below
        public struct RpcAttributeParams
        {
            public RpcDelivery Delivery;
            public bool RequireOwnership;
            public bool DeferLocal;
            public bool AllowTargetOverride;
        }

        // Must match the fields in RemoteAttributeParams
        /// <summary>
        /// Type of RPC delivery method
        /// </summary>
        public RpcDelivery Delivery = RpcDelivery.Reliable;
        public bool RequireOwnership;
        public bool DeferLocal;
        public bool AllowTargetOverride;

        public RpcAttribute(SendTo target)
        {
        }

        // To get around an issue with the release validator, RuntimeAccessModifiersILPP will make this 'public'
        private RpcAttribute()
        {

        }
    }

    /// <summary>
    /// <para>Marks a method as ServerRpc.</para>
    /// <para>A ServerRpc marked method will be fired by a client but executed on the server.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerRpcAttribute : RpcAttribute
    {
        public new bool RequireOwnership;

        public ServerRpcAttribute() : base(SendTo.Server)
        {

        }
    }

    /// <summary>
    /// <para>Marks a method as ClientRpc.</para>
    /// <para>A ClientRpc marked method will be fired by the server but executed on clients.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRpcAttribute : RpcAttribute
    {
        public ClientRpcAttribute() : base(SendTo.NotServer)
        {

        }
    }
}
