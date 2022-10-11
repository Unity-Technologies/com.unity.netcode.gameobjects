using System.Collections;
using System.Collections.Generic;
//using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class RpcManyClientsObject : NetworkBehaviour
    {
        public static RpcManyClientsObject ServerInstance;
        public static Dictionary<ulong, RpcManyClientsObject> ClientInstances = new Dictionary<ulong, RpcManyClientsObject>();

        public static void Reset()
        {
            ServerInstance = null;
            ClientInstances.Clear();
        }

        public int Count = 0;
        public List<ulong> ReceivedFrom = new List<ulong>();


        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ServerInstance = this;
            }
            else
            {
                if (!ClientInstances.ContainsKey(NetworkManager.LocalClientId))
                {
                    ClientInstances.Add(NetworkManager.LocalClientId, this);
                }
            }

            base.OnNetworkSpawn();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResponseServerRpc(ServerRpcParams rpcParams = default)
        {
            ReceivedFrom.Add(rpcParams.Receive.SenderClientId);
            Count++;
        }

        [ClientRpc]
        public void NoParamsClientRpc()
        {
            ResponseServerRpc();
        }

        [ClientRpc]
        public void OneParamClientRpc(int value)
        {
            ResponseServerRpc();
        }

        [ClientRpc]
        public void TwoParamsClientRpc(int value1, int value2)
        {
            ResponseServerRpc();
        }

        [ClientRpc]
        public void WithParamsClientRpc(ClientRpcParams param)
        {
            ResponseServerRpc();
        }
    }

    public class RpcManyClientsTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 10;

        private GameObject m_PrefabToSpawn;

        protected override IEnumerator OnSetup()
        {
            RpcManyClientsObject.Reset();
            return base.OnSetup();
        }

        protected override IEnumerator OnTearDown()
        {
            if (m_PrefabToSpawn != null)
            {
                Object.Destroy(m_PrefabToSpawn);
                m_PrefabToSpawn = null;
            }
            return base.OnTearDown();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("RpcTestMany");
            m_PrefabToSpawn.AddComponent<RpcManyClientsObject>();
        }


        private bool WaitForInstancesToSpawn()
        {
            if (RpcManyClientsObject.ServerInstance == null)
            {
                return false;
            }

            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                if (!RpcManyClientsObject.ClientInstances.ContainsKey(clientNetworkManager.LocalClientId))
                {
                    return false;
                }
            }

            return true;
        }

        private bool WaitForAllClientsToSendServerRpc()
        {
            var serverInstance = RpcManyClientsObject.ServerInstance;
            if (TotalClients != serverInstance.Count)
            {
                return false;
            }

            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                if (!serverInstance.ReceivedFrom.Contains(clientNetworkManager.LocalClientId))
                {
                    return false;
                }
            }
            return true;
        }


        public enum RpcTestTypes
        {
            NoParameters,
            OneParameter,
            TwoParameters,
            WithParameters,
        }

        [UnityTest]
        public IEnumerator RpcManyClientsTest([Values] RpcTestTypes rpcTestType)
        {
            m_EnableVerboseDebug = true;
            VerboseDebug($"[RpcManyClientsTest-{rpcTestType}] Starting Test...");
            var spawnedObject = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            // Wait for the server and all clients to have spawned the test object
            yield return WaitForConditionOrTimeOut(WaitForInstancesToSpawn);
            AssertOnTimeout("Timed out waiting for all instances to spawn!");

            var messageHookList = new List<MessageHookEntry>();
            var serverMessageHookEntry = new MessageHookEntry(m_ServerNetworkManager);
            serverMessageHookEntry.AssignMessageType<ServerRpcMessage>();
            messageHookList.Add(serverMessageHookEntry);
            foreach (var client in m_ClientNetworkManagers)
            {
                var clientMessageHookEntry = new MessageHookEntry(client);
                clientMessageHookEntry.AssignMessageType<ServerRpcMessage>();
                messageHookList.Add(clientMessageHookEntry);
            }
            var rpcMessageHooks = new MessageHooksConditional(messageHookList);

            var rpcManyClientsObject = RpcManyClientsObject.ServerInstance;
            var param = new ClientRpcParams();

            switch (rpcTestType)
            {
                case RpcTestTypes.NoParameters:
                    {
                        rpcManyClientsObject.Count = 0;
                        rpcManyClientsObject.NoParamsClientRpc(); // RPC with no params

                        //VerboseDebug($"[RpcManyClientsTest-{rpcTestType}] Waiting for message hooks...");
                        // Check that all ServerRpcMessages were sent
                        //yield return WaitForConditionOrTimeOut(rpcMessageHooks);

                        VerboseDebug($"[RpcManyClientsTest-{rpcTestType}] Waiting for all clients to have processed ClientRpc and responded with a ServerRpc");
                        // Now provide a small window of time to let the server receive and process all messages
                        yield return WaitForConditionOrTimeOut(WaitForAllClientsToSendServerRpc);
                        AssertOnTimeout($"Timed out wait for {nameof(rpcManyClientsObject.NoParamsClientRpc)}! Only {rpcManyClientsObject.Count} of {TotalClients} was received!");
                        break;
                    }
                case RpcTestTypes.OneParameter:
                    {
                        rpcManyClientsObject.Count = 0;
                        rpcMessageHooks.Reset();
                        rpcManyClientsObject.OneParamClientRpc(0); // RPC with one param
                        //VerboseDebug($"[RpcManyClientsTest-{rpcTestType}] Waiting for message hooks...");
                        //yield return WaitForConditionOrTimeOut(rpcMessageHooks);

                        VerboseDebug($"[RpcManyClientsTest-{rpcTestType}] Waiting for all clients to have processed ClientRpc and responded with a ServerRpc");
                        // Now provide a small window of time to let the server receive and process all messages
                        yield return WaitForConditionOrTimeOut(WaitForAllClientsToSendServerRpc);
                        AssertOnTimeout($"Timed out wait for {nameof(rpcManyClientsObject.OneParamClientRpc)}! Only {rpcManyClientsObject.Count} of {TotalClients} was received!");
                        break;
                    }
                case RpcTestTypes.TwoParameters:
                    {
                        rpcManyClientsObject.Count = 0;
                        rpcMessageHooks.Reset();
                        rpcManyClientsObject.TwoParamsClientRpc(0, 0); // RPC with two params
                        //VerboseDebug($"[RpcManyClientsTest-{rpcTestType}] Waiting for message hooks...");
                        //yield return WaitForConditionOrTimeOut(rpcMessageHooks);

                        VerboseDebug($"[RpcManyClientsTest-{rpcTestType}] Waiting for all clients to have processed ClientRpc and responded with a ServerRpc");
                        // Now provide a small window of time to let the server receive and process all messages
                        yield return WaitForConditionOrTimeOut(WaitForAllClientsToSendServerRpc);
                        AssertOnTimeout($"Timed out wait for {nameof(rpcManyClientsObject.TwoParamsClientRpc)}! Only {rpcManyClientsObject.Count} of {TotalClients} was received!");
                        break;
                    }
                case RpcTestTypes.WithParameters:
                    {
                        rpcManyClientsObject.ReceivedFrom.Clear();
                        rpcManyClientsObject.Count = 0;
                        var target = new List<ulong> { m_ClientNetworkManagers[1].LocalClientId, m_ClientNetworkManagers[2].LocalClientId };
                        param.Send.TargetClientIds = target;

                        //messageHookList.Clear();
                        //var targetedClientMessageHookEntry = new MessageHookEntry(m_ClientNetworkManagers[1]);
                        //targetedClientMessageHookEntry.AssignMessageType<ServerRpcMessage>();
                        //messageHookList.Add(targetedClientMessageHookEntry);
                        //targetedClientMessageHookEntry = new MessageHookEntry(m_ClientNetworkManagers[2]);
                        //targetedClientMessageHookEntry.AssignMessageType<ServerRpcMessage>();
                        //messageHookList.Add(targetedClientMessageHookEntry);
                        //rpcMessageHooks = new MessageHooksConditional(messageHookList);

                        rpcManyClientsObject.WithParamsClientRpc(param);
                        //VerboseDebug($"[RpcManyClientsTest-{rpcTestType}] Waiting for message hooks...");
                        //yield return WaitForConditionOrTimeOut(rpcMessageHooks);

                        VerboseDebug($"[RpcManyClientsTest-{rpcTestType}] Waiting for all clients to have processed RPC");
                        // Now provide a small window of time to let the server receive and process all messages
                        yield return WaitForConditionOrTimeOut(() => 2 == rpcManyClientsObject.Count);
                        AssertOnTimeout($"Timed out wait for {nameof(rpcManyClientsObject.TwoParamsClientRpc)}! Only {rpcManyClientsObject.Count} of 2 was received!");
                        Assert.True(rpcManyClientsObject.ReceivedFrom.Contains(m_ClientNetworkManagers[1].LocalClientId), $"Did not receive a ServerRpc from client {m_ClientNetworkManagers[1].LocalClientId}");
                        Assert.True(rpcManyClientsObject.ReceivedFrom.Contains(m_ClientNetworkManagers[2].LocalClientId), $"Did not receive a ServerRpc from client {m_ClientNetworkManagers[2].LocalClientId}");


                        // either of the 2 selected clients can reply to the server first, due to network timing
                        //var possibility1 = new List<ulong> { m_ClientNetworkManagers[1].LocalClientId, m_ClientNetworkManagers[2].LocalClientId };
                        //var possibility2 = new List<ulong> { m_ClientNetworkManagers[2].LocalClientId, m_ClientNetworkManagers[1].LocalClientId };
                        //Assert.True(Enumerable.SequenceEqual(rpcManyClientsObject.ReceivedFrom, possibility1) || Enumerable.SequenceEqual(rpcManyClientsObject.ReceivedFrom, possibility2));
                        break;
                    }
            }
        }
    }
}
