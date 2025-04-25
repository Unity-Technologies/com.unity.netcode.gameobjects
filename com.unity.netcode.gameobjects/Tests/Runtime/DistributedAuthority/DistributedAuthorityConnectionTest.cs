using System;
using System.Collections;
using System.Linq;
using System.Net;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class DistributedAuthorityConnectionTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        // Set the network topology to distributed authority for all tests
        protected override NetworkTopologyTypes OnGetNetworkTopologyType() => NetworkTopologyTypes.DistributedAuthority;

        public DistributedAuthorityConnectionTest() : base(HostOrServer.DAHost) { }

        private GameObject m_SpawnObject;

        protected override bool UseCMBService()
        {
            return true;
        }


        /// <summary>
        /// Modify NetworkManager instances for settings specific to tests
        /// </summary>
        protected override void OnServerAndClientsCreated()
        {
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.EnableSceneManagement = false;

                // Validate we are in distributed authority mode with client side spawning and using CMB Service
                Assert.True(client.NetworkConfig.NetworkTopology == NetworkTopologyTypes.DistributedAuthority, "Distributed authority topology is not set!");
                Assert.True(client.CMBServiceConnection, "CMBServiceConnection is not set!");
            }

            // Create a prefab for creating and destroying tests (auto-registers with NetworkManagers)
            m_SpawnObject = CreateNetworkObjectPrefab("TestObject");
        }
        [UnityTest]
        public IEnumerator CreateObjectNew()
        {
            SpawnObject(m_SpawnObject, m_ClientNetworkManagers[0]);

            yield return WaitForConditionOrTimeOut(CheckObjectExists);
            AssertOnTimeout("failed to spawn object!");
        }

        private static readonly string k_TransportHost = GetAddressToBind();
        private static readonly ushort k_TransportPort = GetPortToBind();

        /// <summary>
        /// Configures the port to look for the rust service.
        /// </summary>
        /// <returns>The port from the environment variable "CMB_SERVICE_PORT" if it is set and valid; otherwise uses port 7789</returns>
        private static ushort GetPortToBind()
        {
            var value = Environment.GetEnvironmentVariable("CMB_SERVICE_PORT");
            return ushort.TryParse(value, out var configuredPort) ? configuredPort : (ushort)7789;
        }

        /// <summary>
        /// Configures the address to look for the rust service.
        /// </summary>
        /// <returns>The address from the environment variable "NGO_HOST" if it is set and valid; otherwise uses "127.0.0.1"</returns>
        private static string GetAddressToBind()
        {
            var value = Environment.GetEnvironmentVariable("NGO_HOST") ?? "127.0.0.1";
            return Dns.GetHostAddresses(value).First().ToString();
        }

        [Test]
        public void CanConnectToServer()
        {
            var address = Dns.GetHostAddresses(k_TransportHost).First();
            var endpoint = NetworkEndpoint.Parse(address.ToString(), k_TransportPort);

            var driver = NetworkDriver.Create();
            var connection = driver.Connect(endpoint);

            var start = DateTime.Now;
            var ev = Networking.Transport.NetworkEvent.Type.Empty;
            while (ev != Networking.Transport.NetworkEvent.Type.Connect)
            {
                driver.ScheduleUpdate().Complete();
                ev = driver.PopEventForConnection(connection, out _, out _);

                if (DateTime.Now - start > TimeSpan.FromMilliseconds(100))
                {
                    Assert.Fail("Failed to connect to comb service within time!");
                }
            }

            driver.Disconnect(connection);
        }


        private bool CheckObjectExists()
        {
            foreach (var client in m_ClientNetworkManagers)
            {
                if (!s_GlobalNetworkObjects.ContainsKey(client.LocalClientId))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
