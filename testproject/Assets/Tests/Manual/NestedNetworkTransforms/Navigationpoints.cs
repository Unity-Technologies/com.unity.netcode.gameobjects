using System.Collections.Generic;
using UnityEngine;

namespace TestProject.RuntimeTests
{
    public class Navigationpoints : MonoBehaviour
    {
        public static Navigationpoints Instance;

        public List<GameObject> NavPoints;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
    }
}
