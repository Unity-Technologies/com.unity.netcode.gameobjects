using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

public class SetTeleport : MonoBehaviour
{
    public void Set(Vector3 pos)
    {
        GetComponent<NetworkTransform>().Teleport(pos, transform.rotation.eulerAngles, transform.localScale);
    }

    [CustomEditor(typeof(SetTeleport))]
    public class GameEventEditor : Editor
    {
        private string pos = "position";
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var setterObject = (SetTeleport)target;

            // if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening))
            {
                GUILayout.TextArea($"Current pos: {setterObject.transform.position}");
                pos = GUILayout.TextField(pos);
            }

            if (GUILayout.Button("Set"))
            {
                var posParsed = pos.Split(',');
                setterObject.Set(new Vector3(Convert.ToUInt32(posParsed[0]), Convert.ToUInt32(posParsed[1]), Convert.ToUInt32(posParsed[2])));
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


