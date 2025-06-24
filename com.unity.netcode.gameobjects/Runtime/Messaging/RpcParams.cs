using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Netcode
{
    /// <summary>
    /// Specifies how RPC messages should be handled in terms of local execution timing.
    /// </summary>
    public enum LocalDeferMode
    {
        /// <summary>
        /// Uses the default behavior for RPC message handling.
        /// </summary>
        Default,

        /// <summary>
        /// Defers the local execution of the RPC until the next network tick.
        /// </summary>
        Defer,

        /// <summary>
        /// Executes the RPC immediately on the local client without waiting for network synchronization.
        /// </summary>
        SendImmediate
    }

    /// <summary>
    /// Generic RPC. Defines parameters for sending Remote Procedure Calls (RPCs) in the network system.
    /// </summary>
    public struct RpcSendParams
    {
        /// <summary>
        /// Specifies the target that will receive this RPC.
        /// </summary>
        public BaseRpcTarget Target;

        /// <summary>
        /// Controls how the RPC is handled for local execution timing.
        /// </summary>
        public LocalDeferMode LocalDeferMode;

        /// <summary>
        /// Implicitly converts a BaseRpcTarget to RpcSendParams.
        /// </summary>
        /// <param name="target">The RPC target to convert.</param>
        /// <returns>A new RpcSendParams instance with the specified target.</returns>
        public static implicit operator RpcSendParams(BaseRpcTarget target) => new RpcSendParams { Target = target };

        /// <summary>
        /// Implicitly converts a LocalDeferMode to RpcSendParams.
        /// </summary>
        /// <param name="deferMode">The defer mode to convert.</param>
        /// <returns>A new RpcSendParams instance with the specified defer mode.</returns>
        public static implicit operator RpcSendParams(LocalDeferMode deferMode) => new RpcSendParams { LocalDeferMode = deferMode };
    }

    /// <summary>
    /// The receive parameters for server-side remote procedure calls.
    /// </summary>
    public struct RpcReceiveParams
    {
        /// <summary>
        /// Server-Side RPC<br />
        /// The client identifier of the sender.
        /// </summary>
        public ulong SenderClientId;
    }

    /// <summary>
    /// Server-Side RPC<br />
    /// Can be used with any sever-side remote procedure call.<br />
    /// </summary>
    /// <remarks>
    /// Note: typically this is use primarily for the <see cref="ServerRpcReceiveParams"/>.
    /// </remarks>
    public struct RpcParams
    {
        /// <summary>
        /// The server RPC send parameters (currently a place holder).
        /// </summary>
        public RpcSendParams Send;

        /// <summary>
        /// The client RPC receive parameters provides you with the sender's identifier.
        /// </summary>
        public RpcReceiveParams Receive;

        /// <summary>
        /// Implicitly converts RpcSendParams to RpcParams.
        /// </summary>
        /// <param name="send">The send parameters to convert.</param>
        /// <returns>A new RpcParams instance with the specified send parameters.</returns>
        public static implicit operator RpcParams(RpcSendParams send) => new RpcParams { Send = send };

        /// <summary>
        /// Implicitly converts a BaseRpcTarget to RpcParams.
        /// </summary>
        /// <param name="target">The RPC target to convert.</param>
        /// <returns>A new RpcParams instance with the specified target in its send parameters.</returns>
        public static implicit operator RpcParams(BaseRpcTarget target) => new RpcParams { Send = new RpcSendParams { Target = target } };

        /// <summary>
        /// Implicitly converts a LocalDeferMode to RpcParams.
        /// </summary>
        /// <param name="deferMode">The defer mode to convert.</param>
        /// <returns>A new RpcParams instance with the specified defer mode in its send parameters.</returns>
        public static implicit operator RpcParams(LocalDeferMode deferMode) => new RpcParams { Send = new RpcSendParams { LocalDeferMode = deferMode } };

        /// <summary>
        /// Implicitly converts RpcReceiveParams to RpcParams.
        /// </summary>
        /// <param name="receive">The receive parameters to convert.</param>
        /// <returns>A new RpcParams instance with the specified receive parameters.</returns>
        public static implicit operator RpcParams(RpcReceiveParams receive) => new RpcParams { Receive = receive };
    }

    /// <summary>
    /// Server-Side RPC<br />
    /// Place holder.  <see cref="ServerRpcParams"/><br />
    /// Note: Clients always send to one destination when sending RPCs to the server
    /// so this structure is a place holder.
    /// </summary>
    public struct ServerRpcSendParams
    {
    }

    /// <summary>
    /// The receive parameters for server-side remote procedure calls.
    /// </summary>
    public struct ServerRpcReceiveParams
    {
        /// <summary>
        /// Server-Side RPC<br />
        /// The client identifier of the sender.
        /// </summary>
        public ulong SenderClientId;
    }

    /// <summary>
    /// Server-Side RPC
    /// Can be used with any sever-side remote procedure call.
    /// </summary>
    /// <remarks>
    /// Note: typically this is use primarily for the <see cref="ServerRpcReceiveParams"/>.
    /// </remarks>
    public struct ServerRpcParams
    {
        /// <summary>
        /// The server RPC send parameters (currently a place holder).
        /// </summary>
        public ServerRpcSendParams Send;

        /// <summary>
        /// The client RPC receive parameters provides you with the sender's identifier.
        /// </summary>
        public ServerRpcReceiveParams Receive;
    }

    /// <summary>
    /// Client-Side RPC<br />
    /// The send parameters, when sending client RPCs, provides you wil the ability to
    /// target specific clients as a managed or unmanaged list:<br />
    /// <see cref="TargetClientIds"/> and <see cref="TargetClientIdsNativeArray"/>
    /// </summary>
    public struct ClientRpcSendParams
    {
        /// <summary>
        /// IEnumerable version of target id list - use either this OR TargetClientIdsNativeArray.<br />
        /// Note: Even if you provide a value type such as NativeArray, enumerating it will cause boxing.<br />
        /// If you want to avoid boxing, use TargetClientIdsNativeArray.
        /// </summary>
        public IReadOnlyList<ulong> TargetClientIds;

        /// <summary>
        /// NativeArray version of target id list - use either this OR TargetClientIds.<br />
        /// This option avoids any GC allocations but is a bit trickier to use.
        /// </summary>
        public NativeArray<ulong>? TargetClientIdsNativeArray;
    }

    /// <summary>
    /// Client-Side RPC<br />
    /// Place holder.  <see cref="ServerRpcParams"/><br />
    /// </summary>
    /// <remarks>
    /// Note: Server will always be the sender, so this structure is a place holder.
    /// </remarks>
    public struct ClientRpcReceiveParams
    {
    }

    /// <summary>
    /// Client-Side RPC<br />
    /// Can be used with any client-side remote procedure call.<br />
    /// </summary>
    /// <remarks>
    /// Note: Typically this is used primarily for sending to a specific list
    /// of clients as opposed to the default (all).<br />
    /// <see cref="ClientRpcSendParams"/>
    /// </remarks>
    public struct ClientRpcParams
    {
        /// <summary>
        /// The client RPC send parameters provides you with the ability to send to a specific list of clients.
        /// </summary>
        public ClientRpcSendParams Send;

        /// <summary>
        /// The client RPC receive parameters (currently a place holder).
        /// </summary>
        public ClientRpcReceiveParams Receive;
    }

#pragma warning disable IDE1006 // disable naming rule violation check
    // RuntimeAccessModifiersILPP will make this `public`
    internal struct __RpcParams
#pragma warning restore IDE1006 // restore naming rule violation check
    {
        public RpcParams Ext;
        public ServerRpcParams Server;
        public ClientRpcParams Client;
    }
}
