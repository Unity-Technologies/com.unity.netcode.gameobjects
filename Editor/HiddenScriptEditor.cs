using Unity.Netcode.Components;
#if UNITY_UNET_PRESENT
using Unity.Netcode.Transports.UNET;
#endif
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// Internal use. Hides the script field for the given component.
    /// </summary>
    public class HiddenScriptEditor : UnityEditor.Editor
    {
        private static readonly string[] k_HiddenFields = { "m_Script" };

        /// <summary>
        /// Draws inspector properties without the script field.
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();
            DrawPropertiesExcluding(serializedObject, k_HiddenFields);
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }
    }
#if UNITY_UNET_PRESENT
    /// <summary>
    /// Internal use. Hides the script field for UNetTransport.
    /// </summary>
    [CustomEditor(typeof(UNetTransport), true)]
    public class UNetTransportEditor : HiddenScriptEditor
    {

    }
#endif

    /// <summary>
    /// Internal use. Hides the script field for UnityTransport.
    /// </summary>
    [CustomEditor(typeof(UnityTransport), true)]
    public class UnityTransportEditor : HiddenScriptEditor
    {
        private static readonly string[] k_HiddenFields = { "m_Script", "ConnectionData" };

        private bool m_AllowIncomingConnections;
        private bool m_Initialized;

        private UnityTransport m_UnityTransport;

        private SerializedProperty m_ServerAddressProperty;
        private SerializedProperty m_ServerPortProperty;
        private SerializedProperty m_OverrideBindIpProperty;

        private const string k_LoopbackIpv4 = "127.0.0.1";
        private const string k_LoopbackIpv6 = "::1";
        private const string k_AnyIpv4 = "0.0.0.0";
        private const string k_AnyIpv6 = "::";


        private void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }
            m_Initialized = true;
            m_UnityTransport = (UnityTransport)target;

            var connectionDataProperty = serializedObject.FindProperty(nameof(UnityTransport.ConnectionData));

            m_ServerAddressProperty = connectionDataProperty.FindPropertyRelative(nameof(UnityTransport.ConnectionAddressData.Address));
            m_ServerPortProperty = connectionDataProperty.FindPropertyRelative(nameof(UnityTransport.ConnectionAddressData.Port));
            m_OverrideBindIpProperty = connectionDataProperty.FindPropertyRelative(nameof(UnityTransport.ConnectionAddressData.ServerListenAddress));
        }

        /// <summary>
        /// Draws inspector properties without the script field.
        /// </summary>
        public override void OnInspectorGUI()
        {
            Initialize();
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();
            DrawPropertiesExcluding(serializedObject, k_HiddenFields);
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();

            EditorGUILayout.PropertyField(m_ServerAddressProperty);
            EditorGUILayout.PropertyField(m_ServerPortProperty);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.HelpBox("It's recommended to leave remote connections disabled for local testing to avoid exposing ports on your device.", MessageType.Info);
            bool allowRemoteConnections = m_UnityTransport.ConnectionData.ServerListenAddress != k_LoopbackIpv4 && m_UnityTransport.ConnectionData.ServerListenAddress != k_LoopbackIpv6 && !string.IsNullOrEmpty(m_UnityTransport.ConnectionData.ServerListenAddress);
            allowRemoteConnections = EditorGUILayout.Toggle(new GUIContent("Allow Remote Connections?", $"Bind IP: {m_UnityTransport.ConnectionData.ServerListenAddress}"), allowRemoteConnections);

            bool isIpV6 = m_UnityTransport.ConnectionData.IsIpv6;

            if (!allowRemoteConnections)
            {
                if (m_UnityTransport.ConnectionData.ServerListenAddress != k_LoopbackIpv4 && m_UnityTransport.ConnectionData.ServerListenAddress != k_LoopbackIpv6)
                {
                    if (isIpV6)
                    {
                        m_UnityTransport.ConnectionData.ServerListenAddress = k_LoopbackIpv6;
                    }
                    else
                    {
                        m_UnityTransport.ConnectionData.ServerListenAddress = k_LoopbackIpv4;
                    }
                    EditorUtility.SetDirty(m_UnityTransport);
                }
            }

            using (new EditorGUI.DisabledScope(!allowRemoteConnections))
            {
                string overrideIp = m_UnityTransport.ConnectionData.ServerListenAddress;
                if (overrideIp == k_AnyIpv4 || overrideIp == k_AnyIpv6 || overrideIp == k_LoopbackIpv4 || overrideIp == k_LoopbackIpv6)
                {
                    overrideIp = "";
                }

                overrideIp = EditorGUILayout.TextField("Override Bind IP (optional)", overrideIp);
                if (allowRemoteConnections)
                {
                    if (overrideIp == "")
                    {
                        if (isIpV6)
                        {
                            overrideIp = k_AnyIpv6;
                        }
                        else
                        {
                            overrideIp = k_AnyIpv4;
                        }
                    }

                    if (m_UnityTransport.ConnectionData.ServerListenAddress != overrideIp)
                    {
                        m_UnityTransport.ConnectionData.ServerListenAddress = overrideIp;
                        EditorUtility.SetDirty(m_UnityTransport);
                    }
                }
            }
        }
    }

#if COM_UNITY_MODULES_ANIMATION
    /// <summary>
    /// Internal use. Hides the script field for NetworkAnimator.
    /// </summary>
    [CustomEditor(typeof(NetworkAnimator), true)]
    public class NetworkAnimatorEditor : HiddenScriptEditor
    {

    }
#endif

#if COM_UNITY_MODULES_PHYSICS
    /// <summary>
    /// Internal use. Hides the script field for NetworkRigidbody.
    /// </summary>
    [CustomEditor(typeof(NetworkRigidbody), true)]
    public class NetworkRigidbodyEditor : HiddenScriptEditor
    {

    }
#endif

#if COM_UNITY_MODULES_PHYSICS2D
    /// <summary>
    /// Internal use. Hides the script field for NetworkRigidbody2D.
    /// </summary>
    [CustomEditor(typeof(NetworkRigidbody2D), true)]
    public class NetworkRigidbody2DEditor : HiddenScriptEditor
    {

    }
#endif
}
