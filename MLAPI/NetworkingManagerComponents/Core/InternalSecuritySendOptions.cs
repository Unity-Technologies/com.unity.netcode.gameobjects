namespace MLAPI.Internal
{
    internal struct InternalSecuritySendOptions
    {
        internal bool encrypted;
        internal bool authenticated;

        internal InternalSecuritySendOptions(bool encrypted, bool authenticated)
        {
            this.encrypted = encrypted;
            this.authenticated = authenticated;
        }
    }
}