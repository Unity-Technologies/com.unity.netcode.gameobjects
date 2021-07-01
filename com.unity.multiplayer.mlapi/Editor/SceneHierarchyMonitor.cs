using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MLAPI.Editor
{
    [InitializeOnLoad]
    public static class SceneHierarchyMonitor
    {
        static SceneHierarchyMonitor()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        public static readonly List<SceneAsset> CurrentScenesInHierarchy = new List<SceneAsset>();

        public static  void RefreshHierarchy()
        {
            OnHierarchyChanged();
        }



        static private void OnHierarchyChanged()
        {
            var all = Resources.FindObjectsOfTypeAll(typeof(SceneAsset));
            if (all.Count() != CurrentScenesInHierarchy.Count())
            {
                CurrentScenesInHierarchy.Clear();
                foreach (var sceneAsset in all)
                {
                    if (!CurrentScenesInHierarchy.Contains(sceneAsset))
                    {
                        CurrentScenesInHierarchy.Add(sceneAsset as SceneAsset);
                    }
                }
            }
        }
    }
}
