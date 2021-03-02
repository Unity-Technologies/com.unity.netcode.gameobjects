namespace MLAPI.Configuration
{
    /// <summary>
    /// A static class containing MLAPI constants
    /// </summary>
    internal static class NetworkConstants
    {
        internal const string PROTOCOL_VERSION = "13.0.0";

        internal const byte CONNECTION_REQUEST = 3;
        internal const byte CONNECTION_APPROVED = 4;
        internal const byte ADD_OBJECT = 5;
        internal const byte DESTROY_OBJECT = 6;
        internal const byte SWITCH_SCENE = 7;
        internal const byte CLIENT_SWITCH_SCENE_COMPLETED = 8;
        internal const byte CHANGE_OWNER = 9;
        internal const byte ADD_OBJECTS = 10;
        internal const byte TIME_SYNC = 11;
        internal const byte NETWORK_VARIABLE_DELTA = 12;
        internal const byte NETWORK_VARIABLE_UPDATE = 13;
        internal const byte UNNAMED_MESSAGE = 20;
        internal const byte DESTROY_OBJECTS = 21;
        internal const byte NAMED_MESSAGE = 22;
        internal const byte SERVER_LOG = 23;
        internal const byte SERVER_RPC = 30;
        internal const byte CLIENT_RPC = 31;
        internal const byte INVALID = 32;

        internal static readonly string[] MESSAGE_NAMES =
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