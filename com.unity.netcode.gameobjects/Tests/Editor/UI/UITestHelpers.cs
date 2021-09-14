using Unity.Netcode.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.EditorTests
{
    static class UITestHelpers
    {
        [MenuItem("Netcode/UI/Reset Multiplayer Tools Tip Status")]
        static void ResetMultiplayerToolsTipStatus()
        {
            PlayerPrefs.DeleteKey(NetworkManagerEditor.k_InstallMultiplayerToolsTipDismissed_PlayerPrefKey);
        }
    }
}
