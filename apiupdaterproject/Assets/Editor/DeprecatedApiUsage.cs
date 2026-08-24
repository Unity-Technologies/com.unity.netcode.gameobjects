// NGO 2.x-era editor code. Every type reference below must be rewritten by Unity's API updater to
// its `Unity.Netcode.GameObjects.Editor` equivalent. Do not "fix" this file - it is the input to
// the upgrade test. See ../../README.md.
#pragma warning disable 169 // field is never used

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
