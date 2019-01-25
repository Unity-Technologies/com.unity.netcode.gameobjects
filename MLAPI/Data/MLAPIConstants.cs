namespace MLAPI.Data
{
    /// <summary>
    /// A static class containing MLAPI constants
    /// </summary>
    public static class MLAPIConstants
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string MLAPI_PROTOCOL_VERSION = "6.0.0";

        public const byte MLAPI_CERTIFICATE_HAIL = 0;
        public const byte MLAPI_CERTIFICATE_HAIL_RESPONSE = 1;
        public const byte MLAPI_GREETINGS = 2;
        public const byte MLAPI_CONNECTION_REQUEST = 3;
        public const byte MLAPI_CONNECTION_APPROVED = 4;
        public const byte MLAPI_ADD_OBJECT = 5;
        public const byte MLAPI_CLIENT_DISCONNECT = 6;
        public const byte MLAPI_DESTROY_OBJECT = 7;
        public const byte MLAPI_SWITCH_SCENE = 8;
        public const byte MLAPI_CLIENT_SWITCH_SCENE_COMPLETED = 9;
        public const byte MLAPI_SPAWN_POOL_OBJECT = 10;
        public const byte MLAPI_DESTROY_POOL_OBJECT = 11;
        public const byte MLAPI_CHANGE_OWNER = 12;
        public const byte MLAPI_ADD_OBJECTS = 13;
        public const byte MLAPI_TIME_SYNC = 14;
        public const byte MLAPI_NETWORKED_VAR_DELTA = 15;
        public const byte MLAPI_NETWORKED_VAR_UPDATE = 16;
        public const byte MLAPI_SERVER_RPC = 17;
        public const byte MLAPI_SERVER_RPC_REQUEST = 18;
        public const byte MLAPI_SERVER_RPC_RESPONSE = 19;
        public const byte MLAPI_CLIENT_RPC = 20;
        public const byte MLAPI_CLIENT_RPC_REQUEST = 21;
        public const byte MLAPI_CLIENT_RPC_RESPONSE = 22;
        public const byte MLAPI_CUSTOM_MESSAGE = 23;
        public const byte INVALID = 32;
        
        public static readonly string[] MESSAGE_NAMES = {
            "MLAPI_CERTIFICATE_HAIL", // 0
            "MLAPI_CERTIFICATE_HAIL_RESPONSE",
            "MLAPI_GREETINGS",
            "MLAPI_CONNECTION_REQUEST",
            "MLAPI_CONNECTION_APPROVED",
            "MLAPI_ADD_OBJECT",
            "MLAPI_CLIENT_DISCONNECT",
            "MLAPI_DESTROY_OBJECT",
            "MLAPI_SWITCH_SCENE",
            "MLAPI_CLIENT_SWITCH_SCENE_COMPLETED",
            "MLAPI_SPAWN_POOL_OBJECT",
            "MLAPI_DESTROY_POOL_OBJECT",
            "MLAPI_CHANGE_OWNER",
            "MLAPI_ADD_OBJECTS",
            "MLAPI_TIME_SYNC",
            "MLAPI_NETWORKED_VAR_DELTA",
            "MLAPI_NETWORKED_VAR_UPDATE", // 16
            "MLAPI_SERVER_RPC",
            "MLAPI_SERVER_RPC_REQUEST",
            "MLAPI_SERVER_RPC_RESPONSE",
            "MLAPI_CLIENT_RPC",
            "MLAPI_CLIENT_RPC_REQUEST",
            "MLAPI_CLIENT_RPC_RESPONSE",
            "MLAPI_CUSTOM_MESSAGE",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "INVALID" // 32
        };
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
