namespace MLAPI.Profiling
{
    public static class ProfilerConstants
    {
        public const string Connections = nameof(Connections);
        public const string ReceiveTickRate = nameof(ReceiveTickRate);

        public const string NamedMessagesReceived = nameof(NamedMessagesReceived);
        public const string UnnamedMessagesReceived = nameof(UnnamedMessagesReceived);
        public const string BytesSent = nameof(BytesSent);
        public const string BytesReceived = nameof(BytesReceived);
        public const string NetworkVarsReceived = nameof(NetworkVarsReceived);
        public const string RPCsSent = nameof(RPCsSent);
        public const string RPCsReceived = nameof(RPCsReceived);
        public const string RPCBatchesSent = nameof(RPCBatchesSent);
        public const string RPCBatchesReceived = nameof(RPCBatchesReceived);
        public const string RPCQueueProcessed = nameof(RPCQueueProcessed);
        public const string RPCsInQueueSize = nameof(RPCsInQueueSize);
        public const string RPCsOutQueueSize = nameof(RPCsOutQueueSize);
    }
}