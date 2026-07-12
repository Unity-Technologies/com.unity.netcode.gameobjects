using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Transports.SinglePlayer;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace DocumentationCodeSamples
{
    #region SinglePlayerTransportExample
    /// <summary>
    /// Example of how to start a single player or multiplayer session.
    /// Place this on your NetworkManager's GameObject.
    /// </summary>
    public class SwitchingTransportTypesExample : MonoBehaviour
    {
        public enum StartType
        {
            SinglePlayer,
            Client,
            Host,
            Server
        }

        private UnityTransport m_UnityTransport;
        private SinglePlayerTransport m_SinglePlayerTransport;
        private NetworkManager m_NetworkManager;

        private void Awake()
        {
            m_UnityTransport = GetComponent<UnityTransport>();
            m_SinglePlayerTransport = GetComponent<SinglePlayerTransport>();
            m_NetworkManager = GetComponent<NetworkManager>();
        }

        public bool StartSession(StartType startType)
        {
            var startStatus = false;
            // Set the transport to use before starting
            m_NetworkManager.NetworkConfig.NetworkTransport = startType == StartType.SinglePlayer ? m_SinglePlayerTransport : m_UnityTransport;
            switch (startType)
            {
                case StartType.Host:
                case StartType.SinglePlayer:
                    {
                        // Starting a host or single player is the same
                        startStatus = m_NetworkManager.StartHost();
                        break;
                    }
                case StartType.Server:
                    {
                        startStatus = m_NetworkManager.StartServer();
                        break;
                    }
                case StartType.Client:
                    {
                        startStatus = m_NetworkManager.StartClient();
                        break;
                    }
            }
            return startStatus;
        }
    }
    #endregion

    internal class VerifyNetcodeSessionActive : NetworkBehaviour
    {
        public bool RpcReceived { get; private set; }

        public bool TestNetworkVariableEvent { get; private set; }
        public NetworkVariable<bool> TestNetworkVariable = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
        private void VerifyRpc(RpcParams rpcParams = default)
        {
            RpcReceived = true;
        }

        public void TestConnectivity()
        {
            if (IsOwner)
            {
                VerifyRpc();
                TestNetworkVariable.Value = true;
            }
        }

        protected override void OnNetworkPostSpawn()
        {
            TestNetworkVariable.OnValueChanged += TestNetworkVariableChanged;
            base.OnNetworkPostSpawn();
        }

        private void TestNetworkVariableChanged(bool previous, bool current)
        {
            TestNetworkVariableEvent = true;
        }
    }

    [TestFixture(SessionType.SinglePlayer)]
    [TestFixture(SessionType.MultiPlayer)]
    internal class SwitchingTransportTypesTests : NetcodeIntegrationTest
    {
        public enum SessionType
        {
            SinglePlayer,
            MultiPlayer
        }

        protected override int NumberOfClients => 0;

        // We do not need to test this against CMB.
        protected override bool UseCMBService()
        {
            return false;
        }

        private SessionType m_SessionType;
        public SwitchingTransportTypesTests(SessionType sessionType)
        {
            m_SessionType = sessionType;
        }

        protected override IEnumerator OnSetup()
        {
            m_CanStart = false;
            return base.OnSetup();
        }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<VerifyNetcodeSessionActive>();
            base.OnCreatePlayerPrefab();
        }

        private bool m_CanStart = false;
        protected override bool CanStartServerAndClients()
        {
            return m_CanStart;
        }

        private NetworkManager m_Client;
        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            m_Client = networkManager;
            networkManager.NetworkConfig.EnableSceneManagement = false;
            base.OnNewClientCreated(networkManager);
        }

        [UnityTest]
        public IEnumerator SwitchTransportTest()
        {
            var authority = GetAuthorityNetworkManager();
            authority.NetworkConfig.EnableSceneManagement = false;
            var startType = SwitchingTransportTypesExample.StartType.Host;
            if (m_SessionType == SessionType.SinglePlayer)
            {
                startType = SwitchingTransportTypesExample.StartType.SinglePlayer;
                authority.gameObject.AddComponent<SinglePlayerTransport>();
            }
            m_CanStart = true;
            var example = authority.gameObject.AddComponent<SwitchingTransportTypesExample>();
            Assert.IsTrue(example.StartSession(startType), "Failed to start single player session!");

            if (m_SessionType != SessionType.SinglePlayer)
            {
                yield return CreateAndStartNewClient();
                AssertOnTimeout("Timed out waiting for client to start and connect!");
            }

            var verifyNetcode = authority.LocalClient.PlayerObject.GetComponent<VerifyNetcodeSessionActive>();
            verifyNetcode.TestConnectivity();
            if (m_SessionType != SessionType.SinglePlayer)
            {
                m_Client.LocalClient.PlayerObject.GetComponent<VerifyNetcodeSessionActive>().TestConnectivity();
            }

            yield return WaitForConditionOrTimeOut(VerifyNetcodeSession);
            AssertOnTimeout($"Single player session had netcode related errors:");
        }

        /// <summary>
        /// Verifies that all spawned players received the RPC and NetworkVariable
        /// changed event.
        /// </summary>
        private bool VerifyNetcodeSession(StringBuilder errorLog)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                foreach (var networkObject in networkManager.SpawnManager.SpawnedObjectsList)
                {
                    if (!networkObject.IsPlayerObject)
                    {
                        continue;
                    }
                    var verify = networkObject.gameObject.GetComponent<VerifyNetcodeSessionActive>();

                    if (!verify.RpcReceived)
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{verify.name}] Rpc was not recieved!");
                    }

                    if (!verify.TestNetworkVariableEvent)
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{verify.name}] NetworkVariable.OnValueChanged did not get invoked!");
                    }

                    if (!verify.TestNetworkVariable.Value)
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][{verify.name}] {nameof(VerifyNetcodeSessionActive.TestNetworkVariable)} did not get set!");
                    }
                }
            }
            return errorLog.Length == 0;
        }
    }
}
