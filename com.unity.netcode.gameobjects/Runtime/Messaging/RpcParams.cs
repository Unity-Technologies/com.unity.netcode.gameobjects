using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Netcode
{
    public enum LocalDeferMode
    {
        Default,
        Defer,
        SendImmediate
    }
    /// <summary>
    /// Generic RPC
    /// </summary>
    public struct RpcSendParams
    {
        public BaseRpcTarget Target;

        public LocalDeferMode LocalDeferMode;

        public static implicit operator RpcSendParams(BaseRpcTarget target) => new RpcSendParams { Target = target };
        public static implicit operator RpcSendParams(LocalDeferMode deferMode) => new RpcSendParams { LocalDeferMode = deferMode };
    }

    /// <summary>
    /// The receive parameters for server-side remote procedure calls
    /// </summary>
    public struct RpcReceiveParams
    {
        /// <summary>
        /// Server-Side RPC
        /// The client identifier of the sender
        /// </summary>
        public ulong SenderClientId;
    }

    /// <summary>
    /// Server-Side RPC
    /// Can be used with any sever-side remote procedure call
    /// Note: typically this is use primarily for the <see cref="ServerRpcReceiveParams"/>
    /// </summary>
    public struct RpcParams
    {
        /// <summary>
        /// The server RPC send parameters (currently a place holder)
        /// </summary>
        public RpcSendParams Send;

        /// <summary>
        /// The client RPC receive parameters provides you with the sender's identifier
        /// </summary>
        public RpcReceiveParams Receive;

        public static implicit operator RpcParams(RpcSendParams send) => new RpcParams { Send = send };
        public static implicit operator RpcParams(BaseRpcTarget target) => new RpcParams { Send = new RpcSendParams { Target = target } };
        public static implicit operator RpcParams(LocalDeferMode deferMode) => new RpcParams { Send = new RpcSendParams { LocalDeferMode = deferMode } };
        public static implicit operator RpcParams(RpcReceiveParams receive) => new RpcParams { Receive = receive };
    }

    /// <summary>
    /// Server-Side RPC
    /// Place holder.  <see cref="ServerRpcParams"/>
    /// Note: Clients always send to one destination when sending RPCs to the server
    /// so this structure is a place holder
    /// </summary>
    public struct ServerRpcSendParams
    {
    }

    /// <summary>
    /// The receive parameters for server-side remote procedure calls
    /// </summary>
    public struct ServerRpcReceiveParams
    {
        /// <summary>
        /// Server-Side RPC
        /// The client identifier of the sender
        /// </summary>
        public ulong SenderClientId;
    }

    /// <summary>
    /// Server-Side RPC
    /// Can be used with any sever-side remote procedure call
    /// Note: typically this is use primarily for the <see cref="ServerRpcReceiveParams"/>
    /// </summary>
    public struct ServerRpcParams
    {
        /// <summary>
        /// The server RPC send parameters (currently a place holder)
        /// </summary>
        public ServerRpcSendParams Send;

        /// <summary>
        /// The client RPC receive parameters provides you with the sender's identifier
        /// </summary>
        public ServerRpcReceiveParams Receive;
    }

    /// <summary>
    /// Client-Side RPC
    /// The send parameters, when sending client RPCs, provides you wil the ability to
    /// target specific clients as a managed or unmanaged list:
    /// <see cref="TargetClientIds"/> and <see cref="TargetClientIdsNativeArray"/>
    /// </summary>
    public struct ClientRpcSendParams
    {
        /// <summary>
        /// IEnumerable version of target id list - use either this OR TargetClientIdsNativeArray
        /// Note: Even if you provide a value type such as NativeArray, enumerating it will cause boxing.
        /// If you want to avoid boxing, use TargetClientIdsNativeArray
        /// </summary>
        public IReadOnlyList<ulong> TargetClientIds;

        /// <summary>
        /// NativeArray version of target id list - use either this OR TargetClientIds
        /// This option avoids any GC allocations but is a bit trickier to use.
        /// </summary>
        public NativeArray<ulong>? TargetClientIdsNativeArray;
    }

    /// <summary>
    /// Client-Side RPC
    /// Place holder.  <see cref="ServerRpcParams"/>
    /// Note: Server will always be the sender, so this structure is a place holder
    /// </summary>
    public struct ClientRpcReceiveParams
    {
    }

    /// <summary>
    /// Client-Side RPC
    /// Can be used with any client-side remote procedure call
    /// Note: Typically this is used primarily for sending to a specific list
    /// of clients as opposed to the default (all).
    /// <see cref="ClientRpcSendParams"/>
    /// </summary>
    public struct ClientRpcParams
    {
        /// <summary>
        /// The client RPC send parameters provides you with the ability to send to a specific list of clients
        /// </summary>
        public ClientRpcSendParams Send;

        /// <summary>
        /// The client RPC receive parameters (currently a place holder)
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
