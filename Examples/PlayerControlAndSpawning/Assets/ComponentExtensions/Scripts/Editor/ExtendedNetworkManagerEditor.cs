#if UNITY_EDITOR
using System;
using Unity.Netcode;
using Unity.Netcode.Editor;
using UnityEditor;

/// <summary>
/// The custom editor for the <see cref="ExtendedNetworkManager"/> component.
/// </summary>
[CustomEditor(typeof(ExtendedNetworkManager), true)]
[CanEditMultipleObjects]
public class ExtendedNetworkManagerEditor : NetworkManagerEditor
{
    private SerializedProperty m_ConnectionType;
    private SerializedProperty m_TargetFrameRate;
    private SerializedProperty m_EnableVSync;

    public override void OnEnable()
    {
        m_ConnectionType = serializedObject.FindProperty(nameof(ExtendedNetworkManager.ConnectionType));
        m_TargetFrameRate = serializedObject.FindProperty(nameof(ExtendedNetworkManager.TargetFrameRate));
        m_EnableVSync = serializedObject.FindProperty(nameof(ExtendedNetworkManager.EnableVSync));
        base.OnEnable();
    }

    private void DisplayExtendedNetworkManagerProperties()
    {
        EditorGUILayout.PropertyField(m_ConnectionType);
        EditorGUILayout.PropertyField(m_TargetFrameRate);
        EditorGUILayout.PropertyField(m_EnableVSync);
    }

    public override void OnInspectorGUI()
    {
        var extendedNetworkManager = target as ExtendedNetworkManager;
        // Handle switching the appropriate connection type based on the network topology
        // Host connectio type can be set for client-server and distributed authority
        // Live Service can only be used with distributed authority
        // Client-server can only be used with a host connection type
        var connectionTypes = Enum.GetValues(typeof(ExtendedNetworkManager.ConnectionTypes));
        var connectionType = ExtendedNetworkManager.ConnectionTypes.LiveService;
        if (m_ConnectionType.enumValueIndex > 0 && m_ConnectionType.enumValueIndex < connectionTypes.Length)
        {
            connectionType = (ExtendedNetworkManager.ConnectionTypes)connectionTypes.GetValue(m_ConnectionType.enumValueIndex);
        }
        void SetExpanded(bool expanded) { extendedNetworkManager.ExtendedNetworkManagerExpanded = expanded; };
        DrawFoldOutGroup<ExtendedNetworkManager>(extendedNetworkManager.GetType(), DisplayExtendedNetworkManagerProperties, extendedNetworkManager.ExtendedNetworkManagerExpanded, SetExpanded);

        var updatedConnectedType = (ExtendedNetworkManager.ConnectionTypes)connectionTypes.GetValue(m_ConnectionType.enumValueIndex);
        if (connectionType == updatedConnectedType && updatedConnectedType == ExtendedNetworkManager.ConnectionTypes.LiveService && extendedNetworkManager.NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer)
        {
            extendedNetworkManager.ConnectionType = ExtendedNetworkManager.ConnectionTypes.Host;
        }
        else if (connectionType == ExtendedNetworkManager.ConnectionTypes.Host && updatedConnectedType == ExtendedNetworkManager.ConnectionTypes.LiveService && extendedNetworkManager.NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer)
        {
            extendedNetworkManager.NetworkConfig.NetworkTopology = NetworkTopologyTypes.DistributedAuthority;
        }
        base.OnInspectorGUI();
    }
}
#endif
