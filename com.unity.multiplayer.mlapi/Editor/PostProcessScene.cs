using System.Collections.Generic;
using System.Linq;
using MLAPI;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UnityEditor
{
    public class NetworkScenePostProcess : MonoBehaviour
    {
        [PostProcessScene(int.MaxValue)]
        public static void ProcessScene()
        {
            List<NetworkedObject> traverseSortedObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>().ToList();
            
            traverseSortedObjects.Sort((x, y) =>
            {
                List<int> xSiblingIndex = x.TraversedSiblingIndex();
                List<int> ySiblingIndex = y.TraversedSiblingIndex();

                while (xSiblingIndex.Count > 0 && ySiblingIndex.Count > 0)
                {
                    if (xSiblingIndex[0] < ySiblingIndex[0])
                        return -1;
                    
                    if (xSiblingIndex[0] > ySiblingIndex[0])
                        return 1;

                    xSiblingIndex.RemoveAt(0);
                    ySiblingIndex.RemoveAt(0);
                }
                
                return 0;
            });

            for (ulong i = 0; i < (ulong)traverseSortedObjects.Count; i++)
                traverseSortedObjects[(int)i].NetworkedInstanceId = i;
        }
    }
    
    internal static class PrefabHelpers
    {
        internal static List<int> TraversedSiblingIndex(this NetworkedObject networkedObject)
        {
            List<int> paths = new List<int>();
            
            Transform transform = networkedObject.transform;
            
            while (transform != null)
            {
                paths.Add(transform.GetSiblingIndex());
                transform = transform.parent;
            }

            paths.Reverse();
            
            return paths;
        }
    }
}