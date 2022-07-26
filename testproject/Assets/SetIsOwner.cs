#if UNITY_EDITOR
using System;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class SetIsOwner : MonoBehaviour
{
    public void Set(ulong clientID)
    {
        GetComponent<NetworkObject>().ChangeOwnership(clientID);
    }

    [CustomEditor(typeof(SetIsOwner))]
    public class GameEventEditor : Editor
    {
        private string m_ClientID = "clientID (ulong)";
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var gameEvent = (SetIsOwner)target;


            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening))
            {
                GUILayout.TextArea($"Current owner: {gameEvent.GetComponent<NetworkObject>().OwnerClientId}");
                m_ClientID = GUILayout.TextField(m_ClientID);
            }

            if (GUILayout.Button("Set"))
            {
                gameEvent.Set(Convert.ToUInt64(m_ClientID));
            }
        }
    }

    private void Update()
    {
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening))
        {
            // Debug.Log(NetworkManager.Singleton.LocalClientId);
        }
    }
}
#endif
