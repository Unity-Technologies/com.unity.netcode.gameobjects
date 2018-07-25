namespace MLAPI.Data
{
    /// <summary>
    /// A static class containing MLAPI constants
    /// </summary>
    public static class MLAPIConstants
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string MLAPI_PROTOCOL_VERSION = "2.0.0";

        public const ushort MLAPI_CONNECTION_REQUEST = 0;
        public const ushort MLAPI_CONNECTION_APPROVED = 1;
        public const ushort MLAPI_ADD_OBJECT = 2;
        public const ushort MLAPI_CLIENT_DISCONNECT = 3;
        public const ushort MLAPI_DESTROY_OBJECT = 4;
        public const ushort MLAPI_SWITCH_SCENE = 5;
        public const ushort MLAPI_SPAWN_POOL_OBJECT = 6;
        public const ushort MLAPI_DESTROY_POOL_OBJECT = 7;
        public const ushort MLAPI_CHANGE_OWNER = 8;
        public const ushort MLAPI_ADD_OBJECTS = 9;
        public const ushort MLAPI_TIME_SYNC = 10;
        public const ushort MLAPI_NETWORKED_VAR_DELTA = 11;
        public const ushort MLAPI_NETWORKED_VAR_UPDATE = 12;
        public const ushort MLAPI_SERVER_RPC = 13;
        public const ushort MLAPI_CLIENT_RPC = 14;
        public const ushort MLAPI_CUSTOM_MESSAGE = 15;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
