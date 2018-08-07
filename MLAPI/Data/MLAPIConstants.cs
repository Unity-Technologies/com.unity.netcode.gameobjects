namespace MLAPI.Data
{
    /// <summary>
    /// A static class containing MLAPI constants
    /// </summary>
    public static class MLAPIConstants
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string MLAPI_PROTOCOL_VERSION = "2.1.0";

        public const byte MLAPI_CONNECTION_REQUEST = 0;
        public const byte MLAPI_CONNECTION_APPROVED = 1;
        public const byte MLAPI_ADD_OBJECT = 2;
        public const byte MLAPI_CLIENT_DISCONNECT = 3;
        public const byte MLAPI_DESTROY_OBJECT = 4;
        public const byte MLAPI_SWITCH_SCENE = 5;
        public const byte MLAPI_SPAWN_POOL_OBJECT = 6;
        public const byte MLAPI_DESTROY_POOL_OBJECT = 7;
        public const byte MLAPI_CHANGE_OWNER = 8;
        public const byte MLAPI_ADD_OBJECTS = 9;
        public const byte MLAPI_TIME_SYNC = 10;
        public const byte MLAPI_NETWORKED_VAR_DELTA = 11;
        public const byte MLAPI_NETWORKED_VAR_UPDATE = 12;
        public const byte MLAPI_SERVER_RPC = 13;
        public const byte MLAPI_CLIENT_RPC = 14;
        public const byte MLAPI_CUSTOM_MESSAGE = 15;
        
        public static readonly string[] MESSAGE_NAMES = {
            "MLAPI_CONNECTION_REQUEST",
            "MLAPI_CONNECTION_APPROVED",
            "MLAPI_ADD_OBJECT",
            "MLAPI_CLIENT_DISCONNECT",
            "MLAPI_DESTROY_OBJECT",
            "MLAPI_SWITCH_SCENE",
            "MLAPI_SPAWN_POOL_OBJECT",
            "MLAPI_DESTROY_POOL_OBJECT",
            "MLAPI_CHANGE_OWNER",
            "MLAPI_ADD_OBJECTS",
            "MLAPI_TIME_SYNC",
            "MLAPI_NETWORKED_VAR_DELTA",
            "MLAPI_NETWORKED_VAR_UPDATE",
            "MLAPI_SERVER_RPC",
            "MLAPI_CLIENT_RPC",
            "MLAPI_CUSTOM_MESSAGE"
        };
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
