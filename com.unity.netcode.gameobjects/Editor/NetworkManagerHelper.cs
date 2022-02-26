using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Unity.Netcode.Editor
{
#if UNITY_EDITOR
    /// <summary>
    /// Specialized editor specific NetworkManager code
    /// </summary>
    public class NetworkManagerHelper : NetworkManager.INetworkManagerHelper
    {
        internal static NetworkManagerHelper Singleton;

        // This is primarily to handle IntegrationTest scenarios where more than 1 NetworkManager could exist
        private static Dictionary<NetworkManager, Transform> s_LastKnownNetworkManagerParents = new Dictionary<NetworkManager, Transform>();

        /// <summary>
        /// Initializes the singleton instance and registers for:
        /// Hierarchy changed notification: to notify the user when they nest a NetworkManager
        /// Play mode state change notification: to capture when entering or exiting play mode (currently only exiting)
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeOnload()
        {
            Singleton = new NetworkManagerHelper();
            NetworkManager.NetworkManagerHelper = Singleton;

            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            EditorApplication.hierarchyChanged -= EditorApplication_hierarchyChanged;

            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
            EditorApplication.hierarchyChanged += EditorApplication_hierarchyChanged;
        }

        private static void EditorApplication_playModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            switch (playModeStateChange)
            {
                case PlayModeStateChange.ExitingEditMode:
                    {
                        s_LastKnownNetworkManagerParents.Clear();
                        break;
                    }
            }
        }

        private static void EditorApplication_hierarchyChanged()
        {
            var allNetworkManagers = Resources.FindObjectsOfTypeAll<NetworkManager>();
            foreach (var networkManager in allNetworkManagers)
            {
                networkManager.NetworkManagerCheckForParent();
            }
        }

        /// <summary>
        /// Handles notifying the user, via display dialog window, that they have nested a NetworkManager.
        /// When in edit mode it provides the option to automatically fix the issue
        /// When in play mode it just notifies the user when entering play mode as well as when the user
        /// tries to start a network session while a NetworkManager is still nested.
        /// </summary>
        public bool NotifyUserOfNestedNetworkManager(NetworkManager networkManager, bool ignoreNetworkManagerCache = false, bool editorTest = false)
        {
            var gameObject = networkManager.gameObject;
            var transform = networkManager.transform;
            var isParented = transform.root != transform;

            var message = NetworkManager.GenerateNestedNetworkManagerMessage(transform);
            if (s_LastKnownNetworkManagerParents.ContainsKey(networkManager) && !ignoreNetworkManagerCache)
            {
                // If we have already notified the user, then don't notify them again
                if (s_LastKnownNetworkManagerParents[networkManager] == transform.root)
                {
                    return isParented;
                }
                else // If we are no longer a child, then we can remove ourself from this list
                if (transform.root == gameObject.transform)
                {
                    s_LastKnownNetworkManagerParents.Remove(networkManager);
                }
            }
            if (!EditorApplication.isUpdating && isParented)
            {
                if (!EditorApplication.isPlaying && !editorTest)
                {
                    message += $"Click 'Auto-Fix' to automatically remove it from {transform.root.gameObject.name} or 'Manual-Fix' to fix it yourself in the hierarchy view.";
                    if (EditorUtility.DisplayDialog("Invalid Nested NetworkManager", message, "Auto-Fix", "Manual-Fix"))
                    {
                        transform.parent = null;
                        isParented = false;
                    }
                }
                else
                {
                    Debug.LogError(message);
                }

                if (!s_LastKnownNetworkManagerParents.ContainsKey(networkManager) && isParented)
                {
                    s_LastKnownNetworkManagerParents.Add(networkManager, networkManager.transform.root);
                }
            }
            return isParented;
        }
    }
#endif
}
