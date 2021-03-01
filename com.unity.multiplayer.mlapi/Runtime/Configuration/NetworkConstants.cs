namespace MLAPI.Configuration
{
    /// <summary>
    /// A static class containing MLAPI constants
    /// </summary>
    internal static class NetworkConstants
    {
        internal const string k_PROTOCOL_VERSION = "13.0.0";

        internal const byte k_CONNECTION_REQUEST = 3;
        internal const byte k_CONNECTION_APPROVED = 4;
        internal const byte k_ADD_OBJECT = 5;
        internal const byte k_DESTROY_OBJECT = 6;
        internal const byte k_SWITCH_SCENE = 7;
        internal const byte k_CLIENT_SWITCH_SCENE_COMPLETED = 8;
        internal const byte k_CHANGE_OWNER = 9;
        internal const byte k_ADD_OBJECTS = 10;
        internal const byte k_TIME_SYNC = 11;
        internal const byte k_NETWORK_VARIABLE_DELTA = 12;
        internal const byte k_NETWORK_VARIABLE_UPDATE = 13;
        internal const byte k_UNNAMED_MESSAGE = 20;
        internal const byte k_DESTROY_OBJECTS = 21;
        internal const byte k_NAMED_MESSAGE = 22;
        internal const byte k_SERVER_LOG = 23;
        internal const byte k_SERVER_RPC = 30;
        internal const byte k_CLIENT_RPC = 31;
        internal const byte k_INVALID = 32;

        internal static readonly string[] k_MESSAGE_NAMES =
        {
            "", // 0
            "",
            "",
            "CONNECTION_REQUEST",
            "CONNECTION_APPROVED",
            "ADD_OBJECT",
            "DESTROY_OBJECT",
            "SWITCH_SCENE",
            "CLIENT_SWITCH_SCENE_COMPLETED",
            "CHANGE_OWNER",
            "ADD_OBJECTS",
            "TIME_SYNC",
            "NETWORK_VARIABLE_DELTA",
            "NETWORK_VARIABLE_UPDATE",
            "",
            "",
            "", // 16
            "",
            "",
            "",
            "UNNAMED_MESSAGE",
            "DESTROY_OBJECTS",
            "NAMED_MESSAGE",
            "SERVER_LOG",
            "",
            "",
            "",
            "",
            "",
            "",
            "SERVER_RPC",
            "CLIENT_RPC",
            "INVALID" // 32
        };
    }
}