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
        /// Any connected client can invoke the Rpc.
        /// </summary>
        Everyone = 0,

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
    /// <para>Marks a method as a remote procedure call (RPC).</para>
    /// <para>The marked method will be executed on all game instances defined by the <see cref="SendTo"/> target.</para>
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
            /// When true, only the owner of the object can execute this RPC
            /// </summary>
            /// <remarks>
            /// Deprecated in favor of <see cref="InvokePermission"/>.
            /// </remarks>
            [Obsolete("RequireOwnership is deprecated. Please use InvokePermission instead.")]
            public bool RequireOwnership;

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
        /// Controls who has permission to invoke this RPC. The default setting is <see cref="RpcInvokePermission.Everyone"/>
        /// </summary>
        public RpcInvokePermission InvokePermission;

        /// <summary>
        /// When true, only the owner of the object can execute this RPC
        /// </summary>
        /// <remarks>
        /// Deprecated in favor of <see cref="InvokePermission"/>.
        /// </remarks>
        [Obsolete("RequireOwnership is deprecated. Please use InvokePermission = RpcInvokePermission.Owner or InvokePermission = RpcInvokePermission.Everyone instead.")]
        public bool RequireOwnership;

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
        /// </summary>
        /// <remarks>
        /// <para> Deprecated in favor of using <see cref="RpcAttribute"/> with a <see cref="SendTo.Server"/> target and an <see cref="RpcAttribute.InvokePermission"/>.</para>
        /// <code>
        ///     [ServerRpc(RequireOwnership = false)]
        ///     // is replaced with
        ///     [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        ///     // as InvokePermission has a default setting of RpcInvokePermission.Everyone, you can also use
        ///     [Rpc(SendTo.Server)]
        /// </code>
        /// <code>
        ///     [ServerRpc(RequireOwnership = true)]
        ///     // is replaced with
        ///     [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        /// </code>
        /// </remarks>
        [Obsolete("ServerRpc with RequireOwnership is deprecated. Use [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)] or [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] instead.)]")]
        public new bool RequireOwnership;

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
