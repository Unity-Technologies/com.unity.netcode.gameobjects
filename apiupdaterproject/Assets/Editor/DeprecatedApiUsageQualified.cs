// Update only if new public editor API is added to NGO v2.x.x.
// It is used to validate the upgrade test. See ../../README.md.
#pragma warning disable 169 // Ignore field is never used warnings

using System;
using Cfg = Unity.Netcode.Editor.Configuration;
using ManagerEditor = Unity.Netcode.Editor.NetworkManagerEditor;

namespace ApiUpdaterProject.Editor
{
    internal class DeprecatedApiUsageQualified
    {
        private Unity.Netcode.Editor.NetworkObjectEditor m_FullyQualified;
        private Unity.Netcode.Editor.NetcodeEditorBase<ApiUpdaterProject.UpgradeProbeBehaviour> m_FullyQualifiedGeneric;
        private Cfg.NetworkPrefabProcessor m_ThroughNamespaceAlias;
        private ManagerEditor m_ThroughTypeAlias;

        private Type TransformEditorType => typeof(Unity.Netcode.Editor.NetworkTransformEditor);
    }

    internal class DerivesFromDeprecatedBase : Unity.Netcode.Editor.HiddenScriptEditor
    {
    }
}
