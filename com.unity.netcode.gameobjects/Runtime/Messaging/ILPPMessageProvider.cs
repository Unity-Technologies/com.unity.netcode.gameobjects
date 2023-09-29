using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Netcode
{
    internal struct ILPPMessageProvider : INetworkMessageProvider
    {
#pragma warning disable IDE1006 // disable naming rule violation check
        // This is NOT modified by RuntimeAccessModifiersILPP right now, but is populated by ILPP.
        internal static readonly List<NetworkMessageManager.MessageWithHandler> __network_message_types = new List<NetworkMessageManager.MessageWithHandler>();
#pragma warning restore IDE1006 // restore naming rule violation check

        public List<NetworkMessageManager.MessageWithHandler> GetMessages()
        {
            return __network_message_types;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void NotifyOnPlayStateChange()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                // Clear out the network message types, because ILPP-generated RuntimeInitializeOnLoad code will
                // run again and add more messages to it.
                __network_message_types.Clear();
            }
        }

#endif
    }
}
