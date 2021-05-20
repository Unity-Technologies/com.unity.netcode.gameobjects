using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MLAPI;
using MLAPI.RuntimeTests;
using MLAPI.Serialization;
using MLAPI.Messaging;
using Debug = UnityEngine.Debug;

namespace TestProject.RuntimeTests
{
    public class RpcINetworkSerializable
    {       

        private GameObject m_PlayerPrefab;

        private int m_OriginalTargetFrameRate;

        private UserSerializableClass m_UserSerializableClass;

        private bool m_FinishedTest;

        [SetUp]
        public void SetUp()
        {
            // Just always track the current target frame rate (will be re-applied upon TearDown)
            m_OriginalTargetFrameRate = Application.targetFrameRate;

            // Since we use frame count as a metric, we need to assure it runs at a "common update rate"
            // between platforms (i.e. Ubuntu seems to run at much higher FPS when set to -1)
            if (Application.targetFrameRate < 0 || Application.targetFrameRate > 120)
            {
                Application.targetFrameRate = 120;
            }
        }

        /// <summary>
        /// Tests that INetworkSerializable can be used through RPCs by a user
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator NetworkSerializableTest()
        {
            m_FinishedTest = false;
            var numClients = 1;

            var startTime = Time.realtimeSinceStartup;

            // Create Host and (numClients) clients 
            Assert.True(MultiInstanceHelpers.Create(numClients, out NetworkManager server, out NetworkManager[] clients));

            // Create a default player GameObject to use
            m_PlayerPrefab = new GameObject("Player");
            var networkObject = m_PlayerPrefab.AddComponent<NetworkObject>();
            m_PlayerPrefab.AddComponent<TestSerializationComponent>(); 


            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);

            // [Host-Side] Set the player prefab
            server.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            foreach (var client in clients)
            {
                client.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            }

            // Start the instances
            if (!MultiInstanceHelpers.Start(true, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // [Client-Side] Wait for a connection to the server 
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients, null, 512));

            // [Host-Side] Check to make sure all clients are connected
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(server, clients.Length + 1, null, 512));

            // [Client-Side] We only need to get the client side Player's NetworkObject so we can grab that instance of the TestSerializationComponent
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == clients[0].LocalClientId), clients[0], clientClientPlayerResult));
            var clientSideNetworkBehaviourClass = clientClientPlayerResult.Result.gameObject.GetComponent<TestSerializationComponent>();
            clientSideNetworkBehaviourClass.OnSerializableClassUpdated = OnClientReceivedUserSerializableClassUpdated;


            var userSerializableClass = new UserSerializableClass();
            userSerializableClass.MyByteListValues.Add(64);
            userSerializableClass.MyintValue = 1;
            userSerializableClass.MyulongValue = 100;

            clientSideNetworkBehaviourClass.ClientStartTest(userSerializableClass);

            // Wait until the test has finished or we time out
            var timeOutPeriod = Time.realtimeSinceStartup + 5;
            var timedOut = false;
            while(!m_FinishedTest)
            {
                if(Time.realtimeSinceStartup > timeOutPeriod)
                {
                    timedOut = true;
                    break;
                }

                yield return new WaitForSeconds(0.1f);
            }

            // Verify the test passed
            Assert.False(timedOut);
            Assert.IsNotNull(m_UserSerializableClass);
            Assert.AreEqual(m_UserSerializableClass.MyintValue, userSerializableClass.MyintValue + 1);
            Assert.AreEqual(m_UserSerializableClass.MyulongValue, userSerializableClass.MyulongValue + 1);
            Assert.AreEqual(m_UserSerializableClass.MyByteListValues.Count,2);
            Assert.AreEqual(m_UserSerializableClass.MyByteListValues[0], 64);
            Assert.AreEqual(m_UserSerializableClass.MyByteListValues[1], 128);

            // End of test
            clients[0].StopClient();
            server.StopHost();

        }

        private void OnClientReceivedUserSerializableClassUpdated(UserSerializableClass userSerializableClass)
        {
            m_UserSerializableClass = userSerializableClass;
            m_FinishedTest = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_PlayerPrefab != null)
            {
                Object.Destroy(m_PlayerPrefab);
                m_PlayerPrefab = null;
            }

            // Shutdown and clean up both of our NetworkManager instances
            MultiInstanceHelpers.Destroy();

            // Set the application's target frame rate back to its original value
            Application.targetFrameRate = m_OriginalTargetFrameRate;
        }
    }

    public class TestSerializationComponent : NetworkBehaviour
    {
        public delegate void OnSerializableClassUpdatedDelgateHandler(UserSerializableClass userSerializableClass);

        public OnSerializableClassUpdatedDelgateHandler OnSerializableClassUpdated;

        public void ClientStartTest(UserSerializableClass userSerializableClass)
        {
            SendServerSerializedDataServerRpc(userSerializableClass);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendServerSerializedDataServerRpc(UserSerializableClass userSerializableClass)
        {
            userSerializableClass.MyintValue++;
            userSerializableClass.MyulongValue++;
            userSerializableClass.MyByteListValues.Add(128);
            SendClientSerializedDataClientRpc(userSerializableClass);
        }

        [ClientRpc]
        private void SendClientSerializedDataClientRpc(UserSerializableClass userSerializableClass)
        {
            if (OnSerializableClassUpdated != null)
            {
                OnSerializableClassUpdated.Invoke(userSerializableClass);
            }
        }
    }

    public class UserSerializableClass : INetworkSerializable
    {
        public int MyintValue;
        public ulong MyulongValue;
        public List<byte> MyByteListValues;

        public void NetworkSerialize(NetworkSerializer serializer)
        {
            if (serializer.IsReading)
            {
                MyintValue = serializer.Reader.ReadInt32Packed();
                MyulongValue = serializer.Reader.ReadUInt64Packed();
                MyByteListValues = new List<byte>(serializer.Reader.ReadByteArray());
            }
            else
            {
                serializer.Writer.WriteInt32Packed(MyintValue);
                serializer.Writer.WriteUInt64Packed(MyulongValue);
                serializer.Writer.WriteByteArray(MyByteListValues.ToArray());
            }
        }

        public UserSerializableClass()
        {
            MyByteListValues = new List<byte>();
        }
    }
}

