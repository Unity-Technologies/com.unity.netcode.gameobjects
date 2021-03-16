namespace MLAPI.Profiling
{
    public static class ProfilerConstants
    {
        public const string Connections = nameof(Connections);
        public const string ReceiveTickRate = nameof(ReceiveTickRate);

        public const string NamedMessageReceived = nameof(NamedMessageReceived);
        public const string UnnamedMessageReceived = nameof(UnnamedMessageReceived);
        public const string ByteSent = nameof(ByteSent);
        public const string ByteReceived = nameof(ByteReceived);
        public const string NetworkVarReceived = nameof(NetworkVarReceived);
        public const string RpcSent = nameof(RpcSent);
        public const string RpcReceived = nameof(RpcReceived);
        public const string RpcBatchesSent = nameof(RpcBatchesSent);
        public const string RpcBatchesReceived = nameof(RpcBatchesReceived);
        public const string RpcQueueProcessed = nameof(RpcQueueProcessed);
        public const string RpcInQueueSize = nameof(RpcInQueueSize);
        public const string RpcOutQueueSize = nameof(RpcOutQueueSize);
    }
}
