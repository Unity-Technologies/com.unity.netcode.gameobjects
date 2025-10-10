using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;


namespace Unity.Netcode.RuntimeTests
{

    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class NetworkTransformOrderOfOperations : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 4;

        private NetworkObject m_GenericObject;

        private NetworkObject m_ObjectToTest;
        private SpawnSequenceController m_ObjectToTestSeqController;
        private SpawnSequenceController m_AuthoritySeqControllerInstance;

        private NetworkManager m_AuthorityNetworkManager;
        private List<NetworkObject> m_AuthorityGenericInstances = new List<NetworkObject>();

        public NetworkTransformOrderOfOperations(HostOrServer host) : base(host)
        {
        }

        protected override void OnServerAndClientsCreated()
        {
            m_ObjectToTest = CreateNetworkObjectPrefab("TestObject").GetComponent<NetworkObject>();
            m_ObjectToTestSeqController = m_ObjectToTest.gameObject.AddComponent<SpawnSequenceController>();
            m_GenericObject = CreateNetworkObjectPrefab("GenericObject").GetComponent<NetworkObject>();
            base.OnServerAndClientsCreated();
        }

        private bool VerifyGenericsSpawned(StringBuilder errorLog)
        {
            var conditionMet = true;
            foreach (var networkObject in m_AuthorityGenericInstances)
            {
                var networkObjectId = networkObject.NetworkObjectId;
                foreach (var networkManager in m_NetworkManagers)
                {
                    if (networkManager == m_AuthorityNetworkManager)
                    {
                        continue;
                    }

                    if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                    {
                        conditionMet = false;
                        errorLog.AppendLine($"[{networkManager.name}] Has not spawned {networkObject.name}!");
                    }
                }
            }
            return conditionMet;
        }

        private IEnumerator SpawnGenericParents()
        {
            m_AuthorityGenericInstances.Clear();
            for (int i = 0; i < 3; i++)
            {
                var parent = SpawnObject(m_GenericObject.gameObject, m_AuthorityNetworkManager).GetComponent<NetworkObject>();
                m_AuthorityGenericInstances.Add(parent);
            }
            yield return WaitForConditionOrTimeOut(VerifyGenericsSpawned);
            AssertOnTimeout("Failure to spawn generics on one or more clients!");
        }

        [UnityTest]

        public IEnumerator OrderOfOperations()
        {
            m_AuthorityNetworkManager = GetAuthorityNetworkManager();
            yield return SpawnGenericParents();

            ConfigureSequencesTest1(m_AuthorityGenericInstances.First());

            yield return RunTestSequences();

        }

        private void ConfigureSequencesTest1(NetworkObject parent)
        {
            SpawnSequenceController.Clear();
            var teleportSequence = new TeleportSequence()
            {
                Stage = SpawnSequence.SpawnStage.Spawn,
                Position = GetRandomVector3(-10, 10),
                Rotation = (new Quaternion()
                {
                    eulerAngles = GetRandomVector3(-180, 180),
                }),
            };

            var parentSequence = new ParentSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent,
            };

            var changeOwnershipSequence = new ChangeOwnershipSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetOwnerClientId = GetNonAuthorityNetworkManager().LocalClientId,
            };

            SpawnSequenceController.AddAction(parentSequence);
            SpawnSequenceController.AddAction(changeOwnershipSequence);
            SpawnSequenceController.AddAction(teleportSequence);
        }

        private IEnumerator RunTestSequences(bool spawnWithObservers = true)
        {
            m_ObjectToTest.SpawnWithObservers = spawnWithObservers;

            m_AuthoritySeqControllerInstance = SpawnObject(m_ObjectToTest.gameObject, m_AuthorityNetworkManager).GetComponent<SpawnSequenceController>();

            m_AuthoritySeqControllerInstance.AfterSpawn();

            yield return WaitForSpawnedOnAllOrTimeOut(m_AuthoritySeqControllerInstance.NetworkObjectId);
            AssertOnTimeout($"All clients did not spawn {m_AuthoritySeqControllerInstance.name}!");

            yield return WaitForConditionOrTimeOut(TransformsMatch);
            AssertOnTimeout($"Not all {m_AuthoritySeqControllerInstance.name} instances' transforms match!");
        }

        private bool TransformsMatch(StringBuilder errorLog)
        {
            var hasErrors = false;
            var authorityEulerRotation = m_AuthoritySeqControllerInstance.GetSpaceRelativeRotation().eulerAngles;
            var authorityPosition = m_AuthoritySeqControllerInstance.GetSpaceRelativePosition();

            foreach (var networkManager in m_NetworkManagers)
            {
                var nonAuthorityInstance = networkManager.SpawnManager.SpawnedObjects[m_AuthoritySeqControllerInstance.NetworkObjectId].GetComponent<SpawnSequenceController>();
                var nonAuthorityEulerRotation = nonAuthorityInstance.GetSpaceRelativeRotation().eulerAngles;

                var xIsEqual = ApproximatelyEuler(authorityEulerRotation.x, nonAuthorityEulerRotation.x);
                var yIsEqual = ApproximatelyEuler(authorityEulerRotation.y, nonAuthorityEulerRotation.y);
                var zIsEqual = ApproximatelyEuler(authorityEulerRotation.z, nonAuthorityEulerRotation.z);
                if (!xIsEqual || !yIsEqual || !zIsEqual)
                {
                    errorLog.AppendLine($"[Client-{nonAuthorityInstance.NetworkManager.LocalClientId}][{nonAuthorityInstance.gameObject.name}] Rotation {GetVector3Values(nonAuthorityEulerRotation)} does not match the authority rotation {GetVector3Values(authorityEulerRotation)}!");
                    hasErrors = true;
                }
                var nonAuthorityPosition = nonAuthorityInstance.GetSpaceRelativePosition();
                xIsEqual = Approximately(authorityPosition.x, nonAuthorityPosition.x);
                yIsEqual = Approximately(authorityPosition.y, nonAuthorityPosition.y);
                zIsEqual = Approximately(authorityPosition.z, nonAuthorityPosition.z);

                if (!xIsEqual || !yIsEqual || !zIsEqual)
                {
                    errorLog.AppendLine($"[Client-{nonAuthorityInstance.NetworkManager.LocalClientId}][{nonAuthorityInstance.gameObject.name}] Position {GetVector3Values(nonAuthorityPosition)} does not match the authority position {GetVector3Values(authorityPosition)}!");
                    hasErrors = true;
                }
            }
            return !hasErrors;
        }

        internal class NetworkShowSequence : SpawnSequence
        {
            public List<ulong> Clients = new List<ulong>();

            protected override bool OnShouldInvoke(SpawnStage stage)
            {
                return base.OnShouldInvoke(stage) && m_NetworkObject.HasAuthority;
            }

            protected override void OnAction()
            {
                if (Clients.Count == 0)
                {
                    foreach(var clientId in m_NetworkObject.NetworkManager.ConnectedClientsIds)
                    {
                        Clients.Add(clientId);
                    }
                }

                foreach(var clientId in Clients)
                {
                    m_NetworkObject.NetworkShow(clientId);
                }

                base.OnAction();
            }
        }

        internal class TeleportSequence : SpawnSequence
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public enum TeleportContexts
            {
                MotionAuthority,
                ServerLocal,
                OwnerTelportRpc,
            }

            public TeleportContexts TeleportContext;

            private bool m_InvokeOnServer => TeleportContext == TeleportContexts.ServerLocal;
            private bool m_OwnerTeleportRpc => TeleportContext == TeleportContexts.OwnerTelportRpc;

            private bool CanTeleport()
            {
                if (!m_NetworkObject.NetworkManager.DistributedAuthorityMode)
                {
                    var canCommitToTransform = m_SpawnSequenceController.CanCommitToTransform;
                    // With client-server, when we don't want the server to invoke this and the instance can commit to the transform or
                    // we want to invoke on the server or the server should invoke the teleport RPC and it is the server-side instance.
                    return (!m_InvokeOnServer && canCommitToTransform) || ((m_InvokeOnServer || m_OwnerTeleportRpc) && !canCommitToTransform && m_NetworkObject.NetworkManager.IsServer);
                }
                return m_NetworkObject.HasAuthority;
            }

            protected override bool OnShouldInvoke(SpawnStage stage)
            {
                // Check if it is the right stage and if the instance can teleport 
                return base.OnShouldInvoke(stage) && CanTeleport();
            }

            protected override void OnAction()
            {
                m_SpawnSequenceController.SetState(Position, Rotation, teleportDisabled: false);
                base.OnAction();
            }
        }

        internal class ChangeOwnershipSequence : SpawnSequence
        {
            public ulong TargetOwnerClientId;

            private bool CanChangeOwnership()
            {
                if (m_NetworkObject.NetworkManager.DistributedAuthorityMode)
                {
                    return m_NetworkObject.HasAuthority || m_NetworkObject.IsOwnershipTransferable;
                }
                return m_NetworkObject.HasAuthority;
            }

            protected override bool OnShouldInvoke(SpawnStage stage)
            {
                return base.OnShouldInvoke(stage) && CanChangeOwnership();
            }

            protected override void OnAction()
            {
                m_NetworkObject.ChangeOwnership(TargetOwnerClientId);
                base.OnAction();
            }
        }

        internal class ParentSequence : SpawnSequence
        {
            public bool WorldPositionStays = true;
            public NetworkObject TargetParent;

            protected override bool OnShouldInvoke(SpawnStage stage)
            {
                return base.OnShouldInvoke(stage) && (m_NetworkObject.HasAuthority || (m_NetworkObject.IsOwner && m_NetworkObject.AllowOwnerToParent));
            }

            protected override void OnAction()
            {
                var success = TargetParent ? m_NetworkObject.TrySetParent(TargetParent, WorldPositionStays) : m_NetworkObject.TryRemoveParent(WorldPositionStays);
                if (!success)
                {
                    var parentName = TargetParent ? TargetParent.name : "root";
                    Debug.LogError($"[{m_NetworkObject.name}] Failed to parent under {parentName}");
                }
                base.OnAction();
            }
        }

        internal class SpawnSequence
        {
            public enum SpawnStage
            {
                Spawn,
                PostSpawn,
                AfterSpawn
            };

            public SpawnStage Stage;
            protected SpawnSequenceController m_SpawnSequenceController;
            protected NetworkObject m_NetworkObject;

            protected virtual bool OnShouldInvoke(SpawnStage stage)
            {
                return Stage == stage;
            }

            protected virtual void OnAction()
            {

            }

            public void Action(SpawnStage stage, SpawnSequenceController spawnSequenceController)
            {
                m_NetworkObject = spawnSequenceController.NetworkObject;
                m_SpawnSequenceController = spawnSequenceController;
                if (OnShouldInvoke(stage))
                {
                    OnAction();
                }
            }
        }

        public class SpawnSequenceController : NetworkTransform
        {
            // We can get away with using a static list since all instances share the same application domain
            // when running integration tests.
            private static List<SpawnSequence> s_SpawnSequencedActions = new List<SpawnSequence>();

            public static void AddAction(SpawnSequence spawnSequence)
            {
                s_SpawnSequencedActions.Add(spawnSequence);
            }

            public static void Clear()
            {
                s_SpawnSequencedActions.Clear();
            }

            private void InvokeSequencesForStage(SpawnSequence.SpawnStage spawnStage)
            {
                foreach (var action in s_SpawnSequencedActions)
                {
                    action.Action(spawnStage, this);
                }
            }

            public override void OnNetworkSpawn()
            {
                // Must invoke base first in order for CanCommit
                base.OnNetworkSpawn();
                InvokeSequencesForStage(SpawnSequence.SpawnStage.Spawn);
            }

            protected override void OnNetworkPostSpawn()
            {
                Debug.Log($"[{name}] Post spawned on client-{NetworkManager.LocalClientId}");
                InvokeSequencesForStage(SpawnSequence.SpawnStage.PostSpawn);
                base.OnNetworkPostSpawn();
            }

            public void AfterSpawn()
            {
                InvokeSequencesForStage(SpawnSequence.SpawnStage.AfterSpawn);
            }

            [Rpc(SendTo.Owner)]
            public void TeleportRpc(Vector3 position, Quaternion rotation, RpcParams rpcParams = default)
            {
                SetState(posIn: position, rotIn: rotation, teleportDisabled: false);
            }
        }
    }
}
