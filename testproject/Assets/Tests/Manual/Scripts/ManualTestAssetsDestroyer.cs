using UnityEngine;

namespace TestProject.ManualTests
{
    public class ManualTestAssetsDestroyer : MonoBehaviour
    {
        public static bool IsIntegrationTest;

        private void Awake()
        {
            if (IsIntegrationTest)
            {
                Destroy(gameObject);
            }
        }
    }
}
