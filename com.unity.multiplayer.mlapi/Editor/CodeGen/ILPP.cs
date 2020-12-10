using System.IO;
using MLAPI.Messaging;
using MLAPI.Serialization;

namespace MLAPI.Editor.CodeGen.ILPP
{
    internal static class ILPP
    {
        public const string MLAPI_RUNTIME_ASSEMBLY_NAME = "Unity.Multiplayer.MLAPI.Runtime";
        public const string MLAPI_RUNTIME_ASSEMBLY_FILEPATH = "Library/ScriptAssemblies/" + MLAPI_RUNTIME_ASSEMBLY_NAME + ".dll";

        public const string MLAPI_ntableServerRPC_FieldName = "__ntable_ServerRPC";
        public const string MLAPI_ntableClientRPC_FieldName = "__ntable_ClientRPC";
        public const string MLAPI_nregServerRPC_MethodName = "__nreg_ServerRPC";
        public const string MLAPI_nregClientRPC_MethodName = "__nreg_ClientRPC";
        public const string MLAPI_nheadServerRPC_MethodName = "__nhead_ServerRPC";
        public const string MLAPI_nheadClientRPC_MethodName = "__nhead_ClientRPC";
        public const string MLAPI_nwriteServerRPC_MethodName = "__nwrite_ServerRPC";
        public const string MLAPI_nwriteClientRPC_MethodName = "__nwrite_ClientRPC";
        public const string MLAPI_ncallServerRPC_MethodName = "__ncall_ServerRPC";
        public const string MLAPI_ncallClientRPC_MethodName = "__ncall_ClientRPC";

        public static readonly string NetworkBehaviour_FullName = typeof(NetworkedBehaviour).FullName;
        public static readonly string ServerRPC_FullName = typeof(ServerRPCAttribute).FullName;
        public static readonly string ClientRPC_Fullname = typeof(ClientRPCAttribute).FullName;
        public static readonly string BitReader_FullName = typeof(BitReader).FullName;
        public static readonly string BitWriter_FullName = typeof(BitWriter).FullName;
        public static readonly string BitStream_FullName = typeof(BitStream).FullName;
        public static readonly string Stream_FullName = typeof(Stream).FullName;
        public static readonly string InternalMessageSender_FullName = typeof(InternalMessageSender).FullName;
    }
}
