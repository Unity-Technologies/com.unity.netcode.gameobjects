using System;
using System.Collections.Generic;
using MLAPI.Configuration;
using UnityEngine;

namespace MLAPI
{
    [Serializable]
    public class NetworkScriptableObject : ScriptableObject
    {
        internal NetworkObject no;
        private ScriptableObjectNetworkBehaviour nb;

        private class ScriptableObjectNetworkBehaviour : NetworkBehaviour
        {
            [SerializeField]
            internal ScriptableObject referencedScriptableObject;
        }

        internal NetworkPrefab NetworkPrefab { get; set; }

        internal void Init()
        {
            var supportingGO = new GameObject("<SupportingGOForNSO>");
            DontDestroyOnLoad(supportingGO);
            no = supportingGO.AddComponent<NetworkObject>();
            no.GlobalObjectIdHash = 1234; // todo
            nb = supportingGO.AddComponent<ScriptableObjectNetworkBehaviour>();
            nb.referencedScriptableObject = this;
            NetworkPrefab = new NetworkPrefab() { Prefab = supportingGO };
            nb.NetworkObject = no;
            no.ChildNetworkBehaviours = new List<NetworkBehaviour> { nb };
            no.IsSceneObject = null;
            no.DestroyWithScene = false; // todo test with scene switch
            nb.InitializeVariables(GetType(), this, nb);
        }

        void OnValidate()
        {
            if (Application.isPlaying && no != null && no.NetworkManager != null && no.NetworkManager.IsListening)
            {
                nb.SetAllNetworkVariablesDirty();
            }
        }
    }
}
