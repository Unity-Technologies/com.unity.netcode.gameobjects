// The same 2.x API reached through the reference forms the updater has to handle separately from a
// plain `using` + simple name: fully qualified names, a namespace alias, a type alias, a base type
// and a typeof. Do not "fix" this file - it is the input to the upgrade test.
#pragma warning disable 169 // field is never used

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
