using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;


namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class BaseReferenceTests : NetcodeIntegrationTest
    {
        protected struct GroupedComponents
        {
            public GameObject GameObject;
            public NetworkObject NetworkObject;
            public TestNetworkBehaviour TestNetworkBehaviour;
        }

        protected override int NumberOfClients => 1;

        protected GameObject m_TestPrefab;

        protected GroupedComponents m_ValidatingInstance
        {
            get;
            private set;
        }
        protected GroupedComponents m_ReferenceToUse
        {
            get;
            private set;
        }

        public BaseReferenceTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override void OnServerAndClientsCreated()
        {
            m_TestPrefab = CreateNetworkObjectPrefab("ReferenceTest");
            m_TestPrefab.AddComponent<TestNetworkBehaviour>();
            base.OnServerAndClientsCreated();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected GroupedComponents GetGroup(GameObject gameObject)
        {
            return new GroupedComponents()
            {
                GameObject = gameObject,
                NetworkObject = gameObject.GetComponent<NetworkObject>(),
                TestNetworkBehaviour = gameObject.GetComponent<TestNetworkBehaviour>()
            };
        }

        protected IEnumerator SpawnTestPrefabInstance(bool spawnSingle = false)
        {
            var authority = GetAuthorityNetworkManager();
            m_ValidatingInstance = GetGroup(SpawnObject(m_TestPrefab, authority));

            yield return WaitForSpawnedOnAllOrTimeOut(m_ValidatingInstance.GameObject);
            AssertOnTimeout($"[{GetType().Name}][Validating Instance] Faild to spawn {m_ValidatingInstance.GameObject.name} on all clients!");

            if (!spawnSingle)
            {
                m_ReferenceToUse = GetGroup(SpawnObject(m_TestPrefab, authority));

                yield return WaitForSpawnedOnAllOrTimeOut(m_ReferenceToUse.GameObject);
                AssertOnTimeout($"[{GetType().Name}][Reference to use] Faild to spawn {m_ReferenceToUse.GameObject.name} on all clients!");
            }
        }

        #region NetworkBehaviour specific conditional methods
        protected bool RpcWasReceivedAndBehaviourValidated(StringBuilder stringBuilder)
        {
            var authorityValidatingId = m_ValidatingInstance.NetworkObject.NetworkObjectId;
            var authorityReferenceId = m_ReferenceToUse.NetworkObject.NetworkObjectId;
            var validatingNetworkObject = (NetworkObject)null;
            var referenceNetworkObject = (NetworkObject)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityValidatingId, out validatingNetworkObject)
                    && networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityReferenceId, out referenceNetworkObject))
                {
                    var validatingBehaviour = validatingNetworkObject.GetComponent<TestNetworkBehaviour>();
                    var referenceBehaviour = referenceNetworkObject.GetComponent<TestNetworkBehaviour>();

                    if (!validatingBehaviour.ReceivedRPC)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} has not received the RPC!");
                        continue;
                    }
                    if (!validatingBehaviour.AcquiredReference)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} failed to acquire the reference!");
                        continue;
                    }
                    if (referenceBehaviour != validatingBehaviour.RpcReceivedBehaviour)
                    {
                        var currentReferenceBehaviour = validatingBehaviour.RpcReceivedBehaviour != null ? validatingBehaviour.RpcReceivedBehaviour.name : "null";
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} expected behaviour reference {referenceBehaviour.name} but was {currentReferenceBehaviour}!");
                    }
                }
                else
                {
                    var wasNotSpawned = validatingNetworkObject == null ? m_ValidatingInstance.GameObject.name : m_ReferenceToUse.GameObject.name;
                    stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} has not yet spawned {wasNotSpawned}!");
                }
                validatingNetworkObject = null;
                referenceNetworkObject = null;
            }

            return stringBuilder.Length == 0;
        }

        protected bool NetworkVariableChangedAndBehaviourValidated(StringBuilder stringBuilder)
        {
            var authorityValidatingId = m_ValidatingInstance.NetworkObject.NetworkObjectId;
            var authorityReferenceId = m_ReferenceToUse.NetworkObject.NetworkObjectId;
            var validatingNetworkObject = (NetworkObject)null;
            var referenceNetworkObject = (NetworkObject)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityValidatingId, out validatingNetworkObject)
                    && networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityReferenceId, out referenceNetworkObject))
                {
                    var validatingBehaviour = validatingNetworkObject.GetComponent<TestNetworkBehaviour>();
                    var referenceBehaviour = referenceNetworkObject.GetComponent<TestNetworkBehaviour>();

                    if (!validatingBehaviour.TestVariableChanged)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} has not received the NetworkVariable update!");
                        continue;
                    }
                    if (!validatingBehaviour.AcquiredReference)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} failed to acquire a reference!");
                        continue;
                    }
                    if (referenceBehaviour != validatingBehaviour.TestVariableBehaviour)
                    {
                        var currentReferenceBehaviour = validatingBehaviour.TestVariableBehaviour != null ? validatingBehaviour.RpcReceivedBehaviour.name : "null";
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} expected behaviour reference {referenceBehaviour.name} but was {currentReferenceBehaviour}!");
                    }
                }
                else
                {
                    var wasNotSpawned = validatingNetworkObject == null ? m_ValidatingInstance.GameObject.name : m_ReferenceToUse.GameObject.name;
                    stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} has not yet spawned {wasNotSpawned}!");
                }
                validatingNetworkObject = null;
                referenceNetworkObject = null;
            }
            return stringBuilder.Length == 0;
        }

        protected bool RpcSerializingNullValidated(StringBuilder stringBuilder)
        {
            var authorityValidatingId = m_ValidatingInstance.NetworkObject.NetworkObjectId;
            var validatingNetworkObject = (NetworkObject)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityValidatingId, out validatingNetworkObject))
                {
                    var validatingBehaviour = validatingNetworkObject.GetComponent<TestNetworkBehaviour>();

                    if (!validatingBehaviour.ReceivedRPC)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} has not received the RPC!");
                        continue;
                    }
                    if (validatingBehaviour.AcquiredReference)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} Acquired a reference when it should not have!");
                        continue;
                    }
                    if (validatingBehaviour.RpcReceivedBehaviour != null)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} expected behaviour reference NULL but was {validatingBehaviour.RpcReceivedBehaviour.name}!");
                    }
                }
                else
                {
                    stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} has not yet spawned {m_ValidatingInstance.GameObject.name}!");
                }
                validatingNetworkObject = null;
            }
            return stringBuilder.Length == 0;
        }

        protected bool NetworkVariableSerializingNullValidated(StringBuilder stringBuilder)
        {
            var authorityValidatingId = m_ValidatingInstance.NetworkObject.NetworkObjectId;
            var validatingNetworkObject = (NetworkObject)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityValidatingId, out validatingNetworkObject))
                {
                    var validatingBehaviour = validatingNetworkObject.GetComponent<TestNetworkBehaviour>();

                    if (!validatingBehaviour.TestVariableChanged)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} has not detected a change in the NetworkVariable!");
                        continue;
                    }
                    if (validatingBehaviour.AcquiredReference)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} Acquired a reference when it should not have!");
                        continue;
                    }
                    if (validatingBehaviour.TestVariableBehaviour != null)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} expected behaviour reference NULL but was {validatingBehaviour.TestVariableBehaviour.name}!");
                    }
                }
                else
                {
                    stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} has not yet spawned {m_ValidatingInstance.GameObject.name}!");
                }
                validatingNetworkObject = null;
            }
            return stringBuilder.Length == 0;
        }
        #endregion

        protected bool NetworkObjectSerializedValidation(StringBuilder stringBuilder)
        {
            var authorityValidatingId = m_ValidatingInstance.NetworkObject.NetworkObjectId;
            var authorityReferenceId = m_ReferenceToUse.NetworkObject.NetworkObjectId;
            var validatingNetworkObject = (NetworkObject)null;
            var referenceNetworkObject = (NetworkObject)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityReferenceId, out referenceNetworkObject) &&
                    networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityValidatingId, out validatingNetworkObject))
                {
                    var validatingBehaviour = validatingNetworkObject.GetComponent<TestNetworkBehaviour>();
                    var referenceBehaviour = referenceNetworkObject.GetComponent<TestNetworkBehaviour>();

                    if (!validatingBehaviour.AcquiredReference)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingNetworkObject.name} failed to acquire the reference!");
                        continue;
                    }
                    if (referenceNetworkObject != validatingBehaviour.RpcReceivedNetworkObject)
                    {
                        var currentReferencedObject = validatingBehaviour.RpcReceivedNetworkObject != null ? validatingBehaviour.RpcReceivedNetworkObject.name : "null";
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} expected {nameof(NetworkObject)} reference " +
                            $"{referenceNetworkObject.name} but was {currentReferencedObject}!");
                    }
                }
                else
                {
                    var wasNotSpawned = validatingNetworkObject == null ? m_ValidatingInstance.GameObject.name : m_ReferenceToUse.GameObject.name;
                    stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} has not yet spawned {wasNotSpawned}!");
                }
                validatingNetworkObject = null;
                referenceNetworkObject = null;
            }

            return stringBuilder.Length == 0;
        }

        protected bool SerializingNullNetworkObjectValidated(StringBuilder stringBuilder)
        {
            var authorityValidatingId = m_ValidatingInstance.NetworkObject.NetworkObjectId;
            var validatingNetworkObject = (NetworkObject)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityValidatingId, out validatingNetworkObject))
                {
                    var validatingBehaviour = validatingNetworkObject.GetComponent<TestNetworkBehaviour>();

                    if (!validatingBehaviour.ReceivedRPC)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} has not received the RPC!");
                        continue;
                    }
                    if (validatingBehaviour.AcquiredReference)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} Acquired a reference when it should not have!");
                        continue;
                    }
                    if (validatingBehaviour.RpcReceivedNetworkObject != null)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} expected {nameof(NetworkObjectReference)} to be NULL but was {validatingBehaviour.RpcReceivedNetworkObject.name}!");
                    }
                }
                else
                {
                    stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} has not yet spawned {m_ValidatingInstance.GameObject.name}!");
                }
                validatingNetworkObject = null;
            }
            return stringBuilder.Length == 0;
        }


        protected class TestNetworkBehaviour : NetworkBehaviour
        {
            public bool ReceivedRPC;
            public bool TestVariableChanged;
            public bool AcquiredReference;

            public NetworkVariable<NetworkBehaviourReference> NetworkBehaviourVariable = new NetworkVariable<NetworkBehaviourReference>();

            public TestNetworkBehaviour TestVariableBehaviour;

            public TestNetworkBehaviour RpcReceivedBehaviour;
            public TestNetworkBehaviour RpcReceived;

            public NetworkVariable<NetworkObjectReference> NetworkObjectVariable = new NetworkVariable<NetworkObjectReference>();

            public NetworkObject TestVariableNetworkObject;
            public GameObject TestVariableGameObject;

            public NetworkObject RpcReceivedNetworkObject;
            public GameObject RpcReceivedGameObject;

            protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
            {
                // Set it to ourself so we can validate serializing null too.
                RpcReceivedBehaviour = this;
                TestVariableBehaviour = null;
                base.OnNetworkPreSpawn(ref networkManager);
            }

            public override void OnNetworkSpawn()
            {
                NetworkBehaviourVariable.OnValueChanged += OnTestVariableChanged;
                NetworkObjectVariable.OnValueChanged += OnNetworkObjectVariableChanged;
                base.OnNetworkSpawn();
            }

            public override void OnNetworkPreDespawn()
            {
                NetworkBehaviourVariable.OnValueChanged -= OnTestVariableChanged;
                NetworkObjectVariable.OnValueChanged -= OnNetworkObjectVariableChanged;
                base.OnNetworkPreDespawn();
            }
            private void OnTestVariableChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
            {
                TestVariableChanged = true;
                AcquiredReference = current.TryGet(out TestVariableBehaviour, NetworkManager);
            }

            private void OnNetworkObjectVariableChanged(NetworkObjectReference previous, NetworkObjectReference current)
            {
                TestVariableChanged = true;
                AcquiredReference = current.TryGet(out TestVariableNetworkObject, NetworkManager);
            }

            [Rpc(SendTo.Everyone)]
            public void SendNetworkBehaviourReferenceRpc(NetworkBehaviourReference value)
            {
                AcquiredReference = value.TryGet(out RpcReceivedBehaviour, NetworkManager);
                ReceivedRPC = true;
            }

            [Rpc(SendTo.Everyone)]
            public void SendNetworkObjectReferenceRpc(NetworkObjectReference value)
            {
                ReceivedRPC = true;
                AcquiredReference = value.TryGet(out RpcReceivedNetworkObject, NetworkManager);
                RpcReceivedGameObject = value;
            }
        }
    }
}
