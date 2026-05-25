using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode.Editor
{
    public class InScenePlacedPrefab : IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            foreach (var networkObject in FindObjects.FromSceneByType<NetworkObject>(scene, true))
            {
                networkObject.InScenePlaced = true;
            }
        }
    }

    public class InScenePlacedPrefabBuilder : AssetPostprocessor
    {
        public void OnPostprocessPrefab(GameObject root)
        {
            var networkObjects = root.GetComponentsInChildren<NetworkObject>(true);
            foreach (var networkObject in networkObjects)
            {
                networkObject.InScenePlaced = false;
            }
        }
    }
}
