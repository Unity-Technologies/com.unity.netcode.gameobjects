using System.Collections.Generic;
using System.Linq;
using MLAPI;
using MLAPI.Logging;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UnityEditor
{
    public class NetworkScenePostProcess : MonoBehaviour
    {
        [PostProcessScene]
        public static void OnPostProcessScene()
        {
            List<NetworkedObject> networkedObjects = FindObjectsOfType<NetworkedObject>().ToList();
            networkedObjects.Sort((n1, n2) => CompareSiblingPaths(GetSiblingsPath(n1.transform), GetSiblingsPath(n2.transform)));

            for (int i = 0; i < networkedObjects.Count; i++)
            {
                networkedObjects[i].SceneId = (uint)i;
                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("PostProcessing for object \"" + networkedObjects[i].name + 
                                                                                       "\" completed on scene \"" + networkedObjects[i].gameObject.scene.name + 
                                                                                       "\" with objectSceneId \"" + i + "\"");
            }
        }
        
        private static List<int> GetSiblingsPath(Transform transform)
        {
            List<int> result = new List<int>();
            while (transform != null)
            {
                result.Add(transform.GetSiblingIndex());
                transform = transform.parent;
            }
            
            result.Reverse();
            return result;
        }

        private static int CompareSiblingPaths(List<int> l1, List<int> l2)
        {
            while (l1.Count > 0 && l2.Count > 0)
            {
                if (l1[0] < l2[0]) return -1;
                if (l1[0] > l2[0]) return 1;
                
                l1.RemoveAt(0);
                l2.RemoveAt(0);
            }
            return 0;
        }
    }
}