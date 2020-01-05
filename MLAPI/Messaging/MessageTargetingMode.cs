namespace MLAPI.Messaging
{
    // Represents how a ClientRPC should be targeted. Used for binary serialization when proxying packets through server.
    internal enum MessageTargetingMode
    {
        SingleClient,
        ExcludedClient,
        MultiClients,
        Everyone
    }
}
