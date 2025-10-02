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
    /// RPC invoke permissions
    /// </summary>
    public enum RpcInvokePermission
    {
        /// <summary>
        /// Anyone can invoke the Rpc.
        /// </summary>
        Anyone = 0,
        /// <summary>
        /// Rpc can only be invoked by the server.
        /// </summary>
        Server,
        /// <summary>
        /// Rpc can only be invoked by the owner of the NetworkBehaviour.
        /// </summary>
        Owner,
    }

    /// <summary>
    /// <para>Represents the common base class for Rpc attributes.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RpcAttribute : Attribute
    {
        /// <summary>
        /// Parameters that define the behavior of an RPC attribute
        /// </summary>
        public struct RpcAttributeParams
        {
            /// <summary>
            /// Specifies the delivery method for the RPC
            /// </summary>
            public RpcDelivery Delivery;

            /// <summary>
            /// Who has network permission to invoke this RPC
            /// </summary>
            public RpcInvokePermission InvokePermission;

            /// <summary>
            /// When true, local execution of the RPC is deferred until the next network tick
            /// </summary>
            public bool DeferLocal;

            /// <summary>
            /// When true, allows the RPC target to be overridden at runtime
            /// </summary>
            public bool AllowTargetOverride;
        }

        // Must match the fields in RemoteAttributeParams
        /// <summary>
        /// Type of RPC delivery method
        /// </summary>
        public RpcDelivery Delivery = RpcDelivery.Reliable;

        /// <summary>
        /// Who has network permission to invoke this RPC
        /// </summary>
        public RpcInvokePermission InvokePermission;

        /// <summary>
        /// When true, local execution of the RPC is deferred until the next network tick
        /// </summary>
        public bool DeferLocal;

        /// <summary>
        /// When true, allows the RPC target to be overridden at runtime
        /// </summary>
        public bool AllowTargetOverride;

        /// <summary>
        /// Initializes a new instance of the RpcAttribute with the specified target
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
        /// This property overrides the base RpcAttribute.RequireOwnership.
        /// </summary>
        public bool RequireOwnership;

        /// <summary>
        /// Initializes a new instance of ServerRpcAttribute that targets the server
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
        /// Initializes a new instance of ClientRpcAttribute that targets all clients except the server
        /// </summary>
        public ClientRpcAttribute() : base(SendTo.NotServer)
        {

        }
    }
}
