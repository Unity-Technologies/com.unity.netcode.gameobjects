using UnityEditor;


namespace Unity.Netcode.Editor
{
    internal class NetcodeForGameObjectsSettings
    {
        internal const string AutoAddNetworkObjectIfNoneExists = "AutoAdd-NetworkObject-When-None-Exist";

        private static NetcodeForGameObjectsSettings s_Instance;

        internal static bool GetAutoAddNetworkObjectSetting()
        {
            if (EditorPrefs.HasKey(AutoAddNetworkObjectIfNoneExists))
            {
                return EditorPrefs.GetBool(AutoAddNetworkObjectIfNoneExists);
            }
            return false;
        }

        internal static void SetAutoAddNetworkObjectSetting(bool autoAddSetting)
        {
            EditorPrefs.SetBool(AutoAddNetworkObjectIfNoneExists, autoAddSetting);
        }

        internal void ResetAutoAddKey()
        {
            EditorPrefs.DeleteKey(AutoAddNetworkObjectIfNoneExists);
        }
    }
}
