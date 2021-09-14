using Unity.Netcode.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.EditorTests
{
    internal static class UITestHelpers
    {
        [MenuItem("Netcode/UI/Reset Multiplayer Tools Tip Status")]
        private static void ResetMultiplayerToolsTipStatus()
        {
            PlayerPrefs.DeleteKey(NetworkManagerEditor.InstallMultiplayerToolsTipDismissedPlayerPrefKey);
        }
    }
}
