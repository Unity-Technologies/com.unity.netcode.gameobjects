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
        private string clientID = "clientID (ulong)";
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var gameEvent = (SetIsOwner)target;


            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening))
            {
                GUILayout.TextArea($"Current owner: {gameEvent.GetComponent<NetworkObject>().OwnerClientId}");
                clientID = GUILayout.TextField(clientID);
            }

            if (GUILayout.Button("Set"))
            {
                gameEvent.Set(Convert.ToUInt64(clientID));
            }
        }
    }

    private void Update()
    {
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening))
        {
            Debug.Log(NetworkManager.Singleton.LocalClientId);
        }
    }
}


