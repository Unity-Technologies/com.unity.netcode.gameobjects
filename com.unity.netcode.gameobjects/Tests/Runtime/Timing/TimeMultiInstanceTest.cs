using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Tests the times of two clients connecting to a server using the SIPTransport (returns 50ms RTT but has no latency simulation)
    /// </summary>
    public class TimeMultiInstanceTest : BaseMultiInstanceTest
    {
        private const double k_AdditionalTimeTolerance = 0.3d; // magic number and in theory not needed but without this mac os test fail in Yamato because it looks like we get random framerate drops during unit test.

        private NetworkTimeState m_ServerState;
        private NetworkTimeState m_Client1State;
        private NetworkTimeState m_Client2State;

        protected override int NbClients => 2;

        // we need to change frame rate which is done in startup so removing this from here and moving into the test.
        public override IEnumerator Setup()
        {
            yield break;
        }

        private void UpdateTimeStates(NetworkManager[] networkManagers)
        {
            var server = networkManagers.First(t => t.IsServer);
            var firstClient = networkManagers.First(t => t.IsClient);
            var secondClient = networkManagers.Last(t => t.IsClient);

            Assert.AreNotEqual(firstClient, secondClient);

            m_ServerState = new NetworkTimeState(server);
            m_Client1State = new NetworkTimeState(firstClient);
            m_Client2State = new NetworkTimeState(secondClient);
        }

        [UnityTest]
        [TestCase(60, 30u, ExpectedResult = null)]
        [TestCase(30, 30u, ExpectedResult = null)]
        [TestCase(40, 30u, ExpectedResult = null)]
        [TestCase(10, 30u, ExpectedResult = null)]
        [TestCase(60, 60u, ExpectedResult = null)]
        [TestCase(60, 10u, ExpectedResult = null)]
        public IEnumerator TestTimeMultiInstance(int targetFrameRate, uint tickRate)
        {
            yield return StartSomeClientsAndServerWithPlayersCustom(true, NbClients, targetFrameRate, tickRate);

            double frameInterval = 1d / targetFrameRate;
            double tickInterval = 1d / tickRate;

            var networkManagers = MultiInstanceHelpers.NetworkManagerInstances.ToArray();

            var server = networkManagers.First(t => t.IsServer);
            var firstClient = networkManagers.First(t => t.IsClient);
            var secondClient = networkManagers.Last(t => t.IsClient);

            Assert.AreNotEqual(firstClient, secondClient);

            // increase the buffer time of client 2 // the values for client 1 are 0.0333/0.0333 here
            secondClient.NetworkTimeSystem.LocalBufferSec = 0.2;
            secondClient.NetworkTimeSystem.ServerBufferSec = 0.1;

            UpdateTimeStates(networkManagers);


            // wait for at least one tick to pass
            yield return new WaitUntil(() => m_ServerState.LocalTime.Tick != server.NetworkTickSystem.LocalTime.Tick);
            yield return new WaitUntil(() => m_Client1State.LocalTime.Tick != firstClient.NetworkTickSystem.LocalTime.Tick);
            yield return new WaitUntil(() => m_Client2State.LocalTime.Tick != secondClient.NetworkTickSystem.LocalTime.Tick);


            var framesToRun = 3d / frameInterval;

            for (int i = 0; i < framesToRun; i++)
            {
                yield return null;

                UpdateTimeStates(networkManagers);

                // compares whether client times have the correct offset to server
                m_ServerState.AssertCheckDifference(m_Client1State, tickInterval, tickInterval, tickInterval * 2 + frameInterval * 2 + k_AdditionalTimeTolerance);
                m_ServerState.AssertCheckDifference(m_Client2State, 0.2, 0.1, tickInterval * 2 + frameInterval * 2 + k_AdditionalTimeTolerance);

                // compares the two client times, only difference should be based on buffering.
                m_Client1State.AssertCheckDifference(m_Client2State, 0.2 - tickInterval, (0.1 - tickInterval), tickInterval * 2 + frameInterval * 2 + k_AdditionalTimeTolerance);
            }
        }

        // This is from BaseMultiInstanceTest but we need a custom version of this to modifiy the config
        private IEnumerator StartSomeClientsAndServerWithPlayersCustom(bool useHost, int nbClients, int targetFrameRate, uint tickRate)
        {
            // Create multiple NetworkManager instances
            if (!MultiInstanceHelpers.Create(nbClients, out NetworkManager server, out NetworkManager[] clients, targetFrameRate))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            m_ClientNetworkManagers = clients;
            m_ServerNetworkManager = server;

            // Create playerPrefab
            m_PlayerPrefab = new GameObject("Player");
            NetworkObject networkObject = m_PlayerPrefab.AddComponent<NetworkObject>();

            /*
             * Normally we would only allow player prefabs to be set to a prefab. Not runtime created objects.
             * In order to prevent having a Resource folder full of a TON of prefabs that we have to maintain,
             * MultiInstanceHelper has a helper function that lets you mark a runtime created object to be
             * treated as a prefab by the Netcode. That's how we can get away with creating the player prefab
             * at runtime without it being treated as a SceneObject or causing other conflicts with the Netcode.
             */
            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObject);

            // Set the player prefab
            server.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.PlayerPrefab = m_PlayerPrefab;
                clients[i].NetworkConfig.TickRate = tickRate;
            }

            server.NetworkConfig.TickRate = tickRate;

            // Start the instances
            if (!MultiInstanceHelpers.Start(useHost, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // Wait for connection on client side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));

            // Wait for connection on server side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(server, useHost ? nbClients + 1 : nbClients));
        }

        private IEnumerator WaitForFrames(int count)
        {
            int nextFrameNumber = Time.frameCount + count;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
        }

        private readonly struct NetworkTimeState : IEquatable<NetworkTimeState>
        {
            public NetworkTime LocalTime { get; }
            public NetworkTime ServerTime { get; }

            public NetworkTimeState(NetworkManager manager)
            {
                LocalTime = manager.NetworkTickSystem.LocalTime;
                ServerTime = manager.NetworkTickSystem.ServerTime;
            }

            public void AssertCheckDifference(NetworkTimeState clientState, double localBufferDifference, double serverBufferDifference, double tolerance)
            {
                var difLocalAbs = Math.Abs(clientState.LocalTime.Time - LocalTime.Time - localBufferDifference);
                var difServerAbs = Math.Abs(ServerTime.Time - clientState.ServerTime.Time - serverBufferDifference);

                Assert.True(difLocalAbs < tolerance, $"localtime difference: {difLocalAbs} bigger than tolerance: {tolerance}");
                Assert.True(difServerAbs < tolerance, $"servertime difference: {difServerAbs} bigger than tolerance: {tolerance}");
            }

            public bool Equals(NetworkTimeState other)
            {
                return LocalTime.Time.Equals(other.LocalTime.Time) && ServerTime.Time.Equals(other.ServerTime.Time);
            }

            public override bool Equals(object obj)
            {
                return obj is NetworkTimeState other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (LocalTime.Time.GetHashCode() * 397) ^ ServerTime.Time.GetHashCode();
                }
            }
        }
    }
}
