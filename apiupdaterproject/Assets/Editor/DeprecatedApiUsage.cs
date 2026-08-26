// Update only if new public editor API is added to NGO v2.x.x.
// It is used to validate the upgrade test. See ../../README.md.
#pragma warning disable 169 // Ignore field is never used warnings

using ApiUpdaterProject;
using Unity.Netcode.Editor;
using Unity.Netcode.Editor.Configuration;

namespace ApiUpdaterProject.Editor
{
    internal class DeprecatedApiUsage
    {
        // Unity.Netcode.Editor -> Unity.Netcode.GameObjects.Editor
        private NetworkPrefabsEditor m_NetworkPrefabsEditor;
        private HiddenScriptEditor m_HiddenScriptEditor;
        private UnityTransportEditor m_UnityTransportEditor;
        private NetworkAnimatorEditor m_NetworkAnimatorEditor;
        private NetworkRigidbodyEditor m_NetworkRigidbodyEditor;
        private NetworkRigidbody2DEditor m_NetworkRigidbody2DEditor;
        private NetcodeEditorBase<UpgradeProbeBehaviour> m_NetcodeEditorBase;
        private NetworkBehaviourEditor m_NetworkBehaviourEditor;
        private NetworkManagerEditor m_NetworkManagerEditor;
        private NetworkManagerHelper m_NetworkManagerHelper;
        private NetworkObjectEditor m_NetworkObjectEditor;
        private NetworkRigidbodyBaseEditor m_NetworkRigidbodyBaseEditor;
        private NetworkTransformEditor m_NetworkTransformEditor;

        // Unity.Netcode.Editor.Configuration -> Unity.Netcode.GameObjects.Editor.Configuration
        private NetcodeForGameObjectsProjectSettings m_ProjectSettings;
        private NetworkPrefabProcessor m_NetworkPrefabProcessor;
    }
}
