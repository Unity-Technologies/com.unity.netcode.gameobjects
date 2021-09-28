#if UNITY_EDITOR
using System;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

public class SetTeleport : MonoBehaviour
{
    public void Set(Vector3 pos)
    {
        // GetComponent<NetworkTransform>().Teleport(pos, transform.rotation, transform.localScale);
        GetComponent<NetworkTransform>().SetState(pos, transform.rotation, transform.localScale, false);
    }

    [CustomEditor(typeof(SetTeleport))]
    public class GameEventEditor : Editor
    {
        private string m_Pos = "position";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var setterObject = (SetTeleport)target;

            GUILayout.TextArea($"Current pos: {setterObject.transform.position}");
            m_Pos = GUILayout.TextField(m_Pos);

            if (GUILayout.Button("Set"))
            {
                var posParsed = m_Pos.Split(',');
                setterObject.Set(new Vector3(Convert.ToUInt32(posParsed[0]), Convert.ToUInt32(posParsed[1]), Convert.ToUInt32(posParsed[2])));
            }
        }
    }
}
#endif
