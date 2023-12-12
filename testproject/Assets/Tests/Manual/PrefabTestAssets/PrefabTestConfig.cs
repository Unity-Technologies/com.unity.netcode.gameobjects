using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace TestProject.ManualTests
{
    public class PrefabTestConfig : MonoBehaviour
    {
        public static PrefabTestConfig Instance { get; private set; }

        public NetworkPrefabsList TestPrefabs;

        public List<GameObject> InScenePlacedObjects;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance != null)
            {
                Instance = null;
            }
        }
    }
}
