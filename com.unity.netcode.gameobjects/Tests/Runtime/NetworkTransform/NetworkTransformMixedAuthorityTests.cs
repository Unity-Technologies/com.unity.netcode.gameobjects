using System.Collections;
using System.Text;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class NetworkTransformMixedAuthorityTests : IntegrationTestWithApproximation
    {
        private const float k_MotionMagnitude = 5.5f;
        private const int k_Iterations = 4;

        protected override int NumberOfClients => 2;

        private StringBuilder m_ErrorMsg = new StringBuilder();

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<NetworkTransform>();

            var childGameObject = new GameObject();
            childGameObject.transform.parent = m_PlayerPrefab.transform;
            var childNetworkTransform = childGameObject.AddComponent<NetworkTransform>();
            childNetworkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            childNetworkTransform.InLocalSpace = true;

            base.OnCreatePlayerPrefab();
        }

        private void MovePlayers()
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                var direction = GetRandomVector3(-1.0f, 1.0f);
                var playerObject = networkManager.LocalClient.PlayerObject;
                var playerObjectId = networkManager.LocalClient.PlayerObject.NetworkObjectId;
                // Server authoritative
                var serverPlayerClone = m_ServerNetworkManager.SpawnManager.SpawnedObjects[playerObjectId];
                serverPlayerClone.transform.position += direction * k_MotionMagnitude;
                // Owner authoritative
                var childTransform = networkManager.LocalClient.PlayerObject.transform.GetChild(0);
                childTransform.localPosition += direction * k_MotionMagnitude;
            }
        }

        private bool AllInstancePositionsMatch()
        {
            m_ErrorMsg.Clear();
            foreach (var networkManager in m_NetworkManagers)
            {
                var playerObject = networkManager.LocalClient.PlayerObject;
                var playerObjectId = networkManager.LocalClient.PlayerObject.NetworkObjectId;
                var serverRootPosition = m_ServerNetworkManager.SpawnManager.SpawnedObjects[playerObjectId].transform.position;
                var ownerChildPosition = networkManager.LocalClient.PlayerObject.transform.GetChild(0).localPosition;
                foreach (var client in m_NetworkManagers)
                {
                    if (client == networkManager)
                    {
                        continue;
                    }
                    var playerClone = client.SpawnManager.SpawnedObjects[playerObjectId];
                    var cloneRootPosition = playerClone.transform.position;
                    var cloneChildPosition = playerClone.transform.GetChild(0).localPosition;

                    if (!Approximately(serverRootPosition, cloneRootPosition))
                    {
                        m_ErrorMsg.AppendLine($"[{playerObject.name}][{playerClone.name}] Root mismatch ({GetVector3Values(serverRootPosition)})({GetVector3Values(cloneRootPosition)})!");
                    }

                    if (!Approximately(ownerChildPosition, cloneChildPosition))
                    {
                        m_ErrorMsg.AppendLine($"[{playerObject.name}][{playerClone.name}] Child mismatch ({GetVector3Values(ownerChildPosition)})({GetVector3Values(cloneChildPosition)})!");
                    }
                }
            }
            return m_ErrorMsg.Length == 0;
        }

        /// <summary>
        /// Client-Server Only
        /// Validates that mixed authority is working properly
        /// Root -- Server Authoritative
        /// |--Child -- Owner Authoritative
        /// </summary>
        [UnityTest]
        public IEnumerator MixedAuthorityTest()
        {
            for (int i = 0; i < k_Iterations; i++)
            {
                MovePlayers();
                yield return WaitForConditionOrTimeOut(AllInstancePositionsMatch);
                AssertOnTimeout($"Transforms failed to synchronize!");
            }
        }
    }
}
