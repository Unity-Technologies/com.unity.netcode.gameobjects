namespace MLAPI.Profiling
{
    public static class ProfilerConstants
    {
        public const string NumberOfConnections = nameof(NumberOfConnections);
        public const string ReceiveTickRate = nameof(ReceiveTickRate);

        public const string NumberOfNamedMessages = nameof(NumberOfNamedMessages);
        public const string NumberOfUnnamedMessages = nameof(NumberOfUnnamedMessages);
        public const string NumberBytesSent = nameof(NumberBytesSent);
        public const string NumberBytesReceived = nameof(NumberBytesReceived);
        public const string NumberNetworkVarsReceived = nameof(NumberNetworkVarsReceived);
        public const string NumberOfRPCsSent = nameof(NumberOfRPCsSent);
        public const string NumberOfRPCsReceived = nameof(NumberOfRPCsReceived);
        public const string NumberOfRPCBatchesSent = nameof(NumberOfRPCBatchesSent);
        public const string NumberOfRPCBatchesReceived = nameof(NumberOfRPCBatchesReceived);
        public const string NumberOfRPCQueueProcessed = nameof(NumberOfRPCQueueProcessed);
        public const string NumberOfRPCsInQueueSize = nameof(NumberOfRPCsInQueueSize);
        public const string NumberOfRPCsOutQueueSize = nameof(NumberOfRPCsOutQueueSize);
    }
}