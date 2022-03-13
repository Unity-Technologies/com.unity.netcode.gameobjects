namespace Unity.Netcode
{
    public enum NetworkVariableReadPermission
    {
        Everyone,
        Owner,
    }

    public enum NetworkVariableWritePermission
    {
        Server,
        Owner
    }
}
