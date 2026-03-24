using System.Collections;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates edge cases with <see cref="NetworkLog"/>
    /// </summary>
    internal class NetworkLogTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;
        private bool m_ServerStopped;

        /// <summary>
        /// Validates that if no <see cref="NetworkManager"/> exists,
        /// you can still use NetworkLog with the caveat when one does
        /// not exist it will only log locally.
        /// (is topology agnostic)
        /// </summary>
        [UnityTest]
        public IEnumerator UseNetworkLogWithNoNetworkManager()
        {
            m_ServerStopped = false;
            var authority = GetAuthorityNetworkManager();
            authority.OnServerStopped += OnServerStopped;
            authority.Shutdown();
            yield return WaitForConditionOrTimeOut(() => m_ServerStopped);
            AssertOnTimeout($"Timed out waiting for {nameof(NetworkManager)} to stop!");
            // Assure it is destroyed.
            UnityEngine.Object.Destroy(authority);
            authority = null;

            // Clear out the singleton to assure NetworkLog has no references to a NetworkManager
            NetworkManager.ResetSingleton();

            // Validate you can use NetworkLog without any NetworkManager instance.
            NetworkLog.LogInfoServer($"Test a message to the server with no  {nameof(NetworkManager)}.");
            // No exceptions thrown is considered passing.
        }

        private void OnServerStopped(bool obj)
        {
            m_ServerStopped = true;
        }
    }
}
