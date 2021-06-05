using System;
using System.Collections.Generic;
using MLAPI.Configuration;
using UnityEngine;

namespace MLAPI
{
    [Serializable]
    public class NetworkScriptableObject : ScriptableObject
    {
        internal NetworkObject supportingNetworkObject;
        private ScriptableObjectNetworkBehaviour m_SupportingNetworkBehaviour;

        private class ScriptableObjectNetworkBehaviour : NetworkBehaviour
        {
            private void Awake()
            {
                NetworkObject.DestroyWithScene = false;
            }

            [SerializeField]
            internal ScriptableObject referencedScriptableObject;
            public override void NetworkStart()
            {
                base.NetworkStart();
                NetworkObject.DestroyWithScene = false;
            }
        }

        internal NetworkPrefab NetworkPrefab { get; set; }

        internal void Init()
        {
            var supportingGO = new GameObject("<SupportingGOForNSO>"); // game object used to do the synchronisation for this ScriptableObject
            DontDestroyOnLoad(supportingGO);
            supportingNetworkObject = supportingGO.AddComponent<NetworkObject>();
            supportingNetworkObject.GlobalObjectIdHash = 1234; // todo
            m_SupportingNetworkBehaviour = supportingGO.AddComponent<ScriptableObjectNetworkBehaviour>();
            m_SupportingNetworkBehaviour.referencedScriptableObject = this;
            NetworkPrefab = new NetworkPrefab() { Prefab = supportingGO };
            m_SupportingNetworkBehaviour.NetworkObject = supportingNetworkObject;
            supportingNetworkObject.ChildNetworkBehaviours = new List<NetworkBehaviour> { m_SupportingNetworkBehaviour };
            supportingNetworkObject.IsSceneObject = null;
            supportingNetworkObject.DestroyWithScene = false; // todo test with scene switch

            // That's the key making this syncing possible. Instead of taking the current NetworkBehaviour's fields, we're taking this SO's fields
            m_SupportingNetworkBehaviour.InitializeVariables(GetType(), this, m_SupportingNetworkBehaviour);
        }

        void OnValidate()
        {
            if (Application.isPlaying && supportingNetworkObject != null && supportingNetworkObject.NetworkManager != null && supportingNetworkObject.NetworkManager.IsListening)
            {
                m_SupportingNetworkBehaviour.SetAllNetworkVariablesDirty();
            }
        }
    }
}
