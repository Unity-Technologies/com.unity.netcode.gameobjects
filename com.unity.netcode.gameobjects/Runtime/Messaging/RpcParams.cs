using Unity.Collections;

namespace Unity.Netcode
{
    public struct ServerRpcSendParams
    {
    }

    public struct ServerRpcReceiveParams
    {
        public ulong SenderClientId;
    }

    public struct ServerRpcParams
    {
        public ServerRpcSendParams Send;
        public ServerRpcReceiveParams Receive;
    }

    public struct ClientRpcSendParams
    {
        /// <summary>
        /// ulong array version of target id list - use either this OR TargetClientIdsNativeArray
        /// </summary>
        public ulong[] TargetClientIds;

        /// <summary>
        /// NativeArray version of target id list - use either this OR TargetClientIds
        /// This option avoids any GC allocations but is a bit trickier to use.
        /// </summary>
        public NativeArray<ulong>? TargetClientIdsNativeArray;
    }

    public struct ClientRpcReceiveParams
    {
    }

    public struct ClientRpcParams
    {
        public ClientRpcSendParams Send;
        public ClientRpcReceiveParams Receive;
    }

#pragma warning disable IDE1006 // disable naming rule violation check
    // RuntimeAccessModifiersILPP will make this `public`
    internal struct __RpcParams
#pragma warning restore IDE1006 // restore naming rule violation check
    {
        public ServerRpcParams Server;
        public ClientRpcParams Client;
    }
}
