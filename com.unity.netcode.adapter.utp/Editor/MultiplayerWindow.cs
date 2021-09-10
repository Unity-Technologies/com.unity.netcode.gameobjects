using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    public class MultiplayerWindow : EditorWindow
    {
        private const string k_PrefsKeyPrefix = "NetcodeGameObjects";

        [MenuItem("Netcode/Simulator Tools")]
        public static void ShowWindow()
        {
            GetWindow<MultiplayerWindow>(false, "Simulator Tools", true);
        }

        private void OnGUI()
        {
            EditorInt("Client send/recv delay (ms)", "ClientDelay", 0, 2000);
            EditorInt("Client send/recv jitter (ms)", "ClientJitter", 0, 200);
            EditorInt("Client packet drop (percentage)", "ClientDropRate", 0, 100);
        }

        private static string GetKey(string subKey)
        {
            return k_PrefsKeyPrefix + "_" + Application.productName + "_" + subKey;
        }

        private int EditorInt(string label, string key = null, int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            string prefsKey = (string.IsNullOrEmpty(key) ? GetKey(label) : GetKey(key));
            int value;
            value = EditorPrefs.GetInt(prefsKey);

            if (value < minValue)
            {
                value = minValue;
            }

            if (value > maxValue)
            {
                value = maxValue;
            }

            value = EditorGUILayout.IntField(label, value);
            if (value < minValue)
            {
                value = minValue;
            }

            if (value > maxValue)
            {
                value = maxValue;
            }

            EditorPrefs.SetInt(prefsKey, value);

            return value;
        }
    }
}
