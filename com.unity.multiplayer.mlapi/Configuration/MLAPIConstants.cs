namespace MLAPI.Configuration
{
    /// <summary>
    /// A static class containing MLAPI constants
    /// </summary>
    internal static class MLAPIConstants
    {
        internal const string MLAPI_PROTOCOL_VERSION = "12.0.0";

        internal const byte MLAPI_CERTIFICATE_HAIL = 0;
        internal const byte MLAPI_CERTIFICATE_HAIL_RESPONSE = 1;
        internal const byte MLAPI_GREETINGS = 2;
        internal const byte MLAPI_CONNECTION_REQUEST = 3;
        internal const byte MLAPI_CONNECTION_APPROVED = 4;
        internal const byte MLAPI_ADD_OBJECT = 5;
        internal const byte MLAPI_DESTROY_OBJECT = 6;
        internal const byte MLAPI_SWITCH_SCENE = 7;
        internal const byte MLAPI_CLIENT_SWITCH_SCENE_COMPLETED = 8;
        internal const byte MLAPI_CHANGE_OWNER = 9;
        internal const byte MLAPI_ADD_OBJECTS = 10;
        internal const byte MLAPI_TIME_SYNC = 11;
        internal const byte MLAPI_NETWORKED_VAR_DELTA = 12;
        internal const byte MLAPI_NETWORKED_VAR_UPDATE = 13;
        internal const byte MLAPI_SERVER_RPC = 14;
        internal const byte MLAPI_SERVER_RPC_REQUEST = 15;
        internal const byte MLAPI_SERVER_RPC_RESPONSE = 16;
        internal const byte MLAPI_CLIENT_RPC = 17;
        internal const byte MLAPI_CLIENT_RPC_REQUEST = 18;
        internal const byte MLAPI_CLIENT_RPC_RESPONSE = 19;
        internal const byte MLAPI_UNNAMED_MESSAGE = 20;
        internal const byte MLAPI_DESTROY_OBJECTS = 21;
        internal const byte MLAPI_NAMED_MESSAGE = 22;
        internal const byte MLAPI_SYNCED_VAR = 23;
        internal const byte MLAPI_SERVER_LOG = 24;
        internal const byte INVALID = 32;

        internal static readonly string[] MESSAGE_NAMES = {
            "MLAPI_CERTIFICATE_HAIL", // 0
            "MLAPI_CERTIFICATE_HAIL_RESPONSE",
            "MLAPI_GREETINGS",
            "MLAPI_CONNECTION_REQUEST",
            "MLAPI_CONNECTION_APPROVED",
            "MLAPI_ADD_OBJECT",
            "MLAPI_DESTROY_OBJECT",
            "MLAPI_SWITCH_SCENE",
            "MLAPI_CLIENT_SWITCH_SCENE_COMPLETED",
            "MLAPI_CHANGE_OWNER",
            "MLAPI_ADD_OBJECTS",
            "MLAPI_TIME_SYNC",
            "MLAPI_NETWORKED_VAR_DELTA",
            "MLAPI_NETWORKED_VAR_UPDATE",
            "MLAPI_SERVER_RPC",
            "MLAPI_SERVER_RPC_REQUEST",
            "MLAPI_SERVER_RPC_RESPONSE", // 16
            "MLAPI_CLIENT_RPC",
            "MLAPI_CLIENT_RPC_REQUEST",
            "MLAPI_CLIENT_RPC_RESPONSE",
            "MLAPI_UNNAMED_MESSAGE",
            "MLAPI_DESTROY_OBJECTS",
            "MLAPI_NAMED_MESSAGE",
            "MLAPI_SYNCED_VAR",
            "MLAPI_SERVER_LOG",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "INVALID" // 32
        };
    }
}
