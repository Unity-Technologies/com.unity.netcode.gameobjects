#if UNIFIED_NETCODE
using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Counts <see cref="GhostBehaviour.PredictionUpdate"/> calls, split into first-time ticks and re-simulated ticks.
    /// </summary>
    internal partial class HybridPredictionProbe : GhostBehaviour
    {
        public GhostField<int> PredictedCounter;
        public int FirstTimeTicks;
        public int ResimulatedTicks;

        public override void PredictionUpdate(PredictionUpdateContext context)
        {
            if (Ghost.World.NetworkTime.IsFirstTimeFullyPredictingTick)
            {
                FirstTimeTicks++;
            }
            else
            {
                ResimulatedTicks++;
            }
            PredictedCounter.Value = PredictedCounter.Value + 1;
        }
    }

    [TestFixture(HostOrServer.UnifiedHost)]
    [TestFixture(HostOrServer.UnifiedServer)]
    internal class HybridPredictionTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_PredictedPrefab;

        public HybridPredictionTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override bool UseUnifiedTests()
        {
            return true;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PredictedPrefab = CreateHybridPrefab("PredictedProbe", true, GhostMode.OwnerPredicted);
            m_PredictedPrefab.AddComponent<HybridPredictionProbe>();
            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// An owner-predicted hybrid prefab is predicted on the owning client, and the client re-simulates
        /// ticks when snapshots arrive even with no added latency.
        /// </summary>
        [UnityTest]
        public IEnumerator OwnerPredictedHybridPrefabResimulatesOnClient()
        {
            var client = m_ClientNetworkManagers[0];
            var serverInstance = SpawnObject(m_PredictedPrefab, client).GetComponent<NetworkObject>();

            yield return WaitForSpawnedOnAllOrTimeOut(serverInstance);
            AssertOnTimeout($"Timed out waiting for {serverInstance.name} to spawn on all clients!");

            var clientProbe = client.SpawnManager.SpawnedObjects[serverInstance.NetworkObjectId].GetComponent<HybridPredictionProbe>();
            yield return WaitForConditionOrTimeOut(() => clientProbe.Ghost.CanWriteState && clientProbe.ResimulatedTicks > 0);
            AssertOnTimeout($"Client never re-simulated {serverInstance.name}! Predicted: {clientProbe.Ghost.CanWriteState}, " +
                $"first-time ticks: {clientProbe.FirstTimeTicks}, re-simulated ticks: {clientProbe.ResimulatedTicks}");

            var serverProbe = serverInstance.GetComponent<HybridPredictionProbe>();
            Debug.Log($"[{m_ServerNetworkManager.name}] first-time: {serverProbe.FirstTimeTicks}, re-simulated: {serverProbe.ResimulatedTicks} | " +
                $"[{client.name}] first-time: {clientProbe.FirstTimeTicks}, re-simulated: {clientProbe.ResimulatedTicks}");
        }
    }
}
#endif
