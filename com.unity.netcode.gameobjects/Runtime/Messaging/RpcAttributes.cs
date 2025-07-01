using System;

namespace Unity.Netcode
{
    /// <summary>
    /// RPC delivery types.
    /// </summary>
    public enum RpcDelivery
    {
        /// <summary>
        /// Reliable delivery.
        /// </summary>
        Reliable = 0,

        /// <summary>
        /// Unreliable delivery.
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
        /// <summary>
        /// Parameters that define the behavior of an RPC.
        /// </summary>
        public struct RpcAttributeParams
        {
            /// <summary>
            /// The delivery method for the RPC.
            /// </summary>
            public RpcDelivery Delivery;

            /// <summary>
            /// When true, only the owner of the object can execute this RPC.
            /// </summary>
            public bool RequireOwnership;

            /// <summary>
            /// When true, local execution of the RPC is deferred until the next network tick.
            /// </summary>
            public bool DeferLocal;

            /// <summary>
            /// When true, allows the RPC target to be overridden at runtime.
            /// </summary>
            public bool AllowTargetOverride;
        }

        // Must match the fields in RemoteAttributeParams
        /// <summary>
        /// Type of RPC delivery method.
        /// </summary>
        public RpcDelivery Delivery = RpcDelivery.Reliable;

        /// <summary>
        /// When true, only the owner of the object can execute this RPC.
        /// </summary>
        public bool RequireOwnership;

        /// <summary>
        /// When true, local execution of the RPC is deferred until the next network tick.
        /// </summary>
        public bool DeferLocal;

        /// <summary>
        /// When true, allows the RPC target to be overridden at runtime.
        /// </summary>
        public bool AllowTargetOverride;

        /// <summary>
        /// Initializes a new instance of the RpcAttribute with the specified target.
        /// </summary>
        /// <param name="target">The target for this RPC</param>
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
        /// <summary>
        /// When true, only the owner of the NetworkObject can invoke this ServerRpc.
        /// </summary>
        public new bool RequireOwnership;

        /// <summary>
        /// Initializes a new instance of ServerRpcAttribute configured to target the server.
        /// </summary>
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
        /// <summary>
        /// Initializes a new instance of ClientRpcAttribute configured to target all non-server clients.
        /// </summary>
        public ClientRpcAttribute() : base(SendTo.NotServer)
        {

        }
    }
}
