using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;


namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// This test validates various spawn sequences to verify message
    /// ordering is preserved and each sequence action is invoked
    /// on non-authority instances in the order they were invoked on the
    /// authority instance (i.e. preserving order of operations).
    /// <see cref="OrderOfOperations"/>: Test entry point.<br />
    /// <see cref="RunTestSequences"/>: Runs a test based a series of sequences configured sequences.<br />
    /// <see cref="SpawnSequence"/>: Derived from to create a spawn sequence.
    /// <see cref="SpawnSequenceController"/>: Iterates through all defined/configured spawn sequences
    /// on both authoritative and non-authoritative instances for a test configuration.
    /// </summary>
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class NetworkTransformOrderOfOperations : IntegrationTestWithApproximation
    {
        #region Configuration, Properties, and Overrides
        protected override int NumberOfClients => 4;

        private NetworkObject m_GenericObject;

        private NetworkObject m_ObjectToTest;
        private SpawnSequenceController m_AuthoritySeqControllerInstance;

        private NetworkManager m_AuthorityNetworkManager;
        private List<NetworkObject> m_AuthorityParentInstances = new List<NetworkObject>();

        public NetworkTransformOrderOfOperations(HostOrServer host) : base(host)
        {
        }

        protected override IEnumerator OnSetup()
        {
            SpawnSequenceController.Clear();
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_ObjectToTest = CreateNetworkObjectPrefab("TestObject").GetComponent<NetworkObject>();
            m_ObjectToTest.AllowOwnerToParent = true;
            m_ObjectToTest.gameObject.AddComponent<SpawnSequenceController>();
            m_GenericObject = CreateNetworkObjectPrefab("GenericObject").GetComponent<NetworkObject>();
            m_GenericObject.gameObject.AddComponent<ReferenceRpcHelper>();
            base.OnServerAndClientsCreated();
        }

        #endregion

        #region (Wait for) Conditional Methods
        private bool VerifyGenericsSpawned(StringBuilder errorLog)
        {
            var conditionMet = true;
            foreach (var networkObject in m_AuthorityParentInstances)
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
            m_AuthorityParentInstances.Clear();
            for (int i = 0; i < 3; i++)
            {
                var instance = Object.Instantiate(m_GenericObject);
                instance.transform.position = GetRandomVector3(-10, 10);
                instance.transform.rotation = new Quaternion()
                {
                    eulerAngles = GetRandomVector3(-180, 180),
                };
                SpawnObjectInstance(instance, m_AuthorityNetworkManager);
                m_AuthorityParentInstances.Add(instance);
            }
            yield return WaitForConditionOrTimeOut(VerifyGenericsSpawned);
            AssertOnTimeout("Failure to spawn generics on one or more clients!");
        }

        private bool TransformsMatch(StringBuilder errorLog)
        {
            var hasErrors = false;
            var authorityEulerRotation = m_AuthoritySeqControllerInstance.GetSpaceRelativeRotation().eulerAngles;
            var authorityPosition = m_AuthoritySeqControllerInstance.GetSpaceRelativePosition();
            var authorityParent = m_AuthoritySeqControllerInstance.transform.parent ? m_AuthoritySeqControllerInstance.transform.parent.GetComponent<NetworkObject>() : null;
            var authParentName = authorityParent ? authorityParent.name : "root";

            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_AuthoritySeqControllerInstance.NetworkObjectId))
                {
                    hasErrors = true;
                    continue;
                }
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

                var nonAuthorityParent = nonAuthorityInstance.transform.parent ? nonAuthorityInstance.transform.parent.GetComponent<NetworkObject>() : null;
                var nonAuthorityParentName = nonAuthorityParent ? nonAuthorityParent.name : "root";
                if (authorityParent != null)
                {
                    if (nonAuthorityParent != null)
                    {
                        if (nonAuthorityParent.NetworkObjectId != authorityParent.NetworkObjectId)
                        {
                            errorLog.AppendLine($"[Client-{nonAuthorityInstance.NetworkManager.LocalClientId}][{nonAuthorityInstance.gameObject.name}] Parent-{nonAuthorityParent.NetworkObjectId} does not match the authority Parent {authParentName}!");
                            hasErrors = true;
                        }
                    }
                    else
                    {
                        errorLog.AppendLine($"[Client-{nonAuthorityInstance.NetworkManager.LocalClientId}][{nonAuthorityInstance.gameObject.name}] Parent root does not match the authority parent {authParentName}!");
                        hasErrors = true;
                    }
                }
                else if (nonAuthorityInstance != null)
                {
                    errorLog.AppendLine($"[Client-{nonAuthorityInstance.NetworkManager.LocalClientId}][{nonAuthorityInstance.gameObject.name}] " +
                        $"{nonAuthorityParentName}-{nonAuthorityParent.NetworkObjectId} does not match the authority parent {authParentName}-{authorityParent.NetworkObjectId}!");
                    hasErrors = true;
                }
            }
            return !hasErrors;
        }
        #endregion

        #region OrderOfOperations Core Test Methods
        [UnityTest]
        public IEnumerator OrderOfOperations()
        {
            m_EnableVerboseDebug = true;
            SpawnSequenceController.VerboseLog = m_EnableVerboseDebug;
            m_AuthorityNetworkManager = GetAuthorityNetworkManager();
            yield return SpawnGenericParents();
            var parent1 = m_AuthorityParentInstances[0];
            var parent2 = m_AuthorityParentInstances[1];

            ConfigureSequencesTest1_ClientServerOnly(parent1);
            yield return RunTestSequences();

            ConfigureSequencesTest2(parent1);
            yield return RunTestSequences();

            ConfigureSequencesTest3(parent1, parent2);
            yield return RunTestSequences();

            ConfigureSequencesTest4(parent1, parent2);
            yield return RunTestSequences(false);

            ConfigureSequencesTest5(parent1);
            yield return RunTestSequences();

            ConfigureSequencesTest6_ClientServerOnly(parent1);
            yield return RunTestSequences();

            ConfigureSequencesTest7_ClientServerOnly(parent1);
            yield return RunTestSequences(spawnWithOwnership: true);

            ConfigureSequencesTest8(parent1);
            yield return RunTestSequences(spawnWithObservers: false);

            ConfigureSequencesTest9(parent1);
            yield return RunTestSequences(spawnWithObservers: false);

            ConfigureSequencesTest10_ClientServerOnly(parent1, parent2);
            yield return RunTestSequences(spawnWithOwnership: true);

            ConfigureSequencesTest11_ClientServerOnly(parent1, parent2);
            yield return RunTestSequences(spawnWithOwnership: true);
        }


        private IEnumerator RunTestSequences(bool spawnWithObservers = true, bool spawnWithOwnership = false)
        {
            yield return __RunTestSequences(spawnWithObservers, spawnWithOwnership);

            // Assure the generic parents are all at the root hierarchy.
            foreach (var parent in m_AuthorityParentInstances)
            {
                parent.transform.parent = null;
            }

            // Reset the controller's global settings
            SpawnSequenceController.Clear();
        }

        private IEnumerator __RunTestSequences(bool spawnWithObservers = true, bool spawnWithOwnership = false)
        {
            // Exit early if we shouldn't run
            if (!SpawnSequenceController.ShouldRun(m_AuthorityNetworkManager))
            {
                VerboseDebug($"Skipping {SpawnSequenceController.CurrentTest}");
                yield break;
            }

            VerboseDebug($"Running {SpawnSequenceController.CurrentTest}");
            var instance = Object.Instantiate(m_ObjectToTest);
            instance.SpawnWithObservers = spawnWithObservers;
            SpawnObjectInstance(instance, spawnWithOwnership ? GetNonAuthorityNetworkManager() : m_AuthorityNetworkManager);
            m_AuthoritySeqControllerInstance = instance.GetComponent<SpawnSequenceController>();
            var authorityObjectId = m_AuthoritySeqControllerInstance.NetworkObjectId;
            m_AuthoritySeqControllerInstance.AfterSpawn();
            if (spawnWithObservers)
            {
                yield return WaitForSpawnedOnAllOrTimeOut(m_AuthoritySeqControllerInstance.NetworkObjectId);
                AssertOnTimeout($"All clients did not spawn {m_AuthoritySeqControllerInstance.name}!");
                foreach (var networkManager in m_NetworkManagers)
                {
                    if (networkManager == m_AuthorityNetworkManager)
                    {
                        continue;
                    }
                    networkManager.SpawnManager.SpawnedObjects[authorityObjectId].GetComponent<SpawnSequenceController>().AfterSpawn();
                }
            }

            // Assure all sequenced actions have been invoked.
            yield return WaitForConditionOrTimeOut(SpawnSequenceController.AllActionsInvoked);
            if (s_GlobalTimeoutHelper.HasTimedOut())
            {
                // If we timed out, then check for pending and if found wait for the condition
                // once more.
                if (SpawnSequenceController.ActionIsPending())
                {
                    yield return WaitForConditionOrTimeOut(SpawnSequenceController.AllActionsInvoked);
                }
            }
            AssertOnTimeout($"[{SpawnSequenceController.CurrentTest}] Not all actions were invoked for the current test sequence!\n {SpawnSequenceController.ErrorLog}");
            yield return WaitForConditionOrTimeOut(TransformsMatch);
            AssertOnTimeout($"Not all {m_AuthoritySeqControllerInstance.name} instances' transforms match!");

            // De-spawn the test object
            if (m_AuthoritySeqControllerInstance.HasAuthority)
            {
                m_AuthoritySeqControllerInstance.NetworkObject.Despawn();
            }
            else
            {
                foreach (var networkManager in m_NetworkManagers)
                {
                    if (networkManager.SpawnManager.SpawnedObjects[m_AuthoritySeqControllerInstance.NetworkObjectId].HasAuthority)
                    {
                        networkManager.SpawnManager.SpawnedObjects[m_AuthoritySeqControllerInstance.NetworkObjectId].Despawn();
                        break;
                    }
                }
            }
        }
        #endregion

        #region Test Sequence Configurations
        /// <summary>
        /// Client server only - under da mode only the authority can parent
        /// Test-1:
        /// Authority-> Spawn, change ownership, (wait), parent
        /// </summary>
        private void ConfigureSequencesTest1_ClientServerOnly(NetworkObject parent)
        {
            SpawnSequenceController.CurrentTest = "Test1 (Client-Server Only)";
            if (m_AuthorityNetworkManager.DistributedAuthorityMode)
            {
                SpawnSequenceController.ClientServerOnly = true;
                return;
            }

            var changeOwnershipSequence = new ChangeOwnershipSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetOwnerClientId = GetNonAuthorityNetworkManager().LocalClientId,
            };

            var parentSequence = new ParentSequence()
            {
                TimeDelayInMS = 200,
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent,
                InvokeOnlyOnClientId = GetAuthorityNetworkManager().LocalClientId,
            };

            SpawnSequenceController.AddAction(changeOwnershipSequence);
            SpawnSequenceController.AddAction(parentSequence);
        }

        /// <summary>
        /// Test-2:
        /// Authority-> Spawn, change parent, change ownership
        /// </summary>
        private void ConfigureSequencesTest2(NetworkObject parent)
        {
            SpawnSequenceController.CurrentTest = "Test2";

            var changeOwnershipSequence = new ChangeOwnershipSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetOwnerClientId = GetNonAuthorityNetworkManager().LocalClientId,
            };

            var parentSequence = new ParentSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent,
            };

            SpawnSequenceController.AddAction(parentSequence);
            SpawnSequenceController.AddAction(changeOwnershipSequence);
        }

        /// <summary>
        /// Test-3:
        /// Authority-> Spawn, change parent(1), change ownership
        /// Client-Owner-> Afterspawn re-parent(2)
        /// </summary>
        private void ConfigureSequencesTest3(NetworkObject parent1, NetworkObject parent2)
        {
            SpawnSequenceController.CurrentTest = "Test3";

            // Authority
            var parentSequence = new ParentSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent1,
            };

            var changeOwnershipSequence = new ChangeOwnershipSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetOwnerClientId = GetNonAuthorityNetworkManager().LocalClientId,
            };

            SpawnSequenceController.AddAction(parentSequence);
            SpawnSequenceController.AddAction(changeOwnershipSequence);

            // Client-Owner
            var parentSequenceClient = new ParentSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent2,
            };
            SpawnSequenceController.AddAction(parentSequenceClient);
        }

        /// <summary>
        /// Test-4:
        /// Authority-> Spawn no observers, change parents(multiple), NetworkShow, change ownership, Teleport RPC
        /// ClientOwner-> Teleport RPC
        /// </summary>
        private void ConfigureSequencesTest4(NetworkObject parent1, NetworkObject parent2)
        {
            SpawnSequenceController.CurrentTest = "Test4";

            // Authority
            var parentSequence1 = new ParentSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent1,
            };

            var parentSequence2 = new ParentSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent2,
            };

            var networkShowSequence = new NetworkShowSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
            };

            var teleportSequence = new TeleportSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TeleportContext = TeleportSequence.TeleportContexts.OwnerTelportRpc,
                Position = GetRandomVector3(-10, 10),
                Rotation = (new Quaternion()
                {
                    eulerAngles = GetRandomVector3(-180, 180),
                }),
                InvokeOnlyOnClientId = m_AuthorityNetworkManager.LocalClientId,
            };

            var changeOwnershipSequence = new ChangeOwnershipSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetOwnerClientId = GetNonAuthorityNetworkManager().LocalClientId,
            };

            SpawnSequenceController.AddAction(parentSequence1);
            SpawnSequenceController.AddAction(parentSequence2);
            SpawnSequenceController.AddAction(networkShowSequence);
            SpawnSequenceController.AddAction(changeOwnershipSequence);
            SpawnSequenceController.AddAction(teleportSequence);
        }

        /// <summary>
        /// Test-5:
        /// Authority-> Spawn, change parent (1), change ownership, Teleport RPC with NetworkBehaviourReference
        /// ClientOwner-> Teleport RPC using NetworkBehaviourReference
        /// </summary>
        private void ConfigureSequencesTest5(NetworkObject parent1)
        {
            SpawnSequenceController.CurrentTest = "Test5";

            // Authority
            var parentSequence1 = new ParentSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent1,
            };

            var changeOwnershipSequence = new ChangeOwnershipSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetOwnerClientId = GetNonAuthorityNetworkManager().LocalClientId,
            };

            var teleportSequence = new ReferenceRpcSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TeleportContext = TeleportSequence.TeleportContexts.OwnerTelportRpc,
                Position = GetRandomVector3(-10, 10),
                Rotation = (new Quaternion()
                {
                    eulerAngles = GetRandomVector3(-180, 180),
                }),
                InvokeOnlyOnClientId = m_AuthorityNetworkManager.LocalClientId,
                ReferenceTeleportHelperId = parent1.GetComponent<ReferenceRpcHelper>().NetworkObjectId,
            };

            SpawnSequenceController.AddAction(parentSequence1);
            SpawnSequenceController.AddAction(changeOwnershipSequence);
            SpawnSequenceController.AddAction(teleportSequence);
        }

        /// <summary>
        /// Test-6: (Client-Server only)
        /// Authority-> Spawn, change ownership, change parent (1), Teleport RPC with NetworkBehaviourReference
        /// ClientOwner-> Teleport RPC using NetworkBehaviourReference
        /// </summary>
        private void ConfigureSequencesTest6_ClientServerOnly(NetworkObject parent1)
        {
            SpawnSequenceController.CurrentTest = "Test6 (Client-Server Only)";
            if (m_AuthorityNetworkManager.DistributedAuthorityMode)
            {
                SpawnSequenceController.ClientServerOnly = true;
                return;
            }

            // Authority
            var parentSequence1 = new ParentSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent1,
            };

            var changeOwnershipSequence = new ChangeOwnershipSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetOwnerClientId = GetNonAuthorityNetworkManager().LocalClientId,
            };

            var teleportSequence = new ReferenceRpcSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TeleportContext = TeleportSequence.TeleportContexts.OwnerTelportRpc,
                Position = GetRandomVector3(-10, 10),
                Rotation = (new Quaternion()
                {
                    eulerAngles = GetRandomVector3(-180, 180),
                }),
                InvokeOnlyOnClientId = m_AuthorityNetworkManager.LocalClientId,
                ReferenceTeleportHelperId = parent1.GetComponent<ReferenceRpcHelper>().NetworkObjectId,
            };

            SpawnSequenceController.AddAction(changeOwnershipSequence);
            SpawnSequenceController.AddAction(parentSequence1);
            SpawnSequenceController.AddAction(teleportSequence);
        }

        /// <summary>
        /// Test-7: (Client-Server only)
        /// Authority-> Spawn with ownership,change parent (1), Teleport RPC with NetworkBehaviourReference
        /// ClientOwner-> Teleport RPC using NetworkBehaviourReference
        /// </summary>
        private void ConfigureSequencesTest7_ClientServerOnly(NetworkObject parent1)
        {
            SpawnSequenceController.CurrentTest = "Test7 (Client-Server Only)";
            if (m_AuthorityNetworkManager.DistributedAuthorityMode)
            {
                SpawnSequenceController.ClientServerOnly = true;
                return;
            }

            // Authority
            var parentSequence1 = new ParentSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent1,
            };

            var teleportSequence = new ReferenceRpcSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TeleportContext = TeleportSequence.TeleportContexts.OwnerTelportRpc,
                Position = GetRandomVector3(-10, 10),
                Rotation = (new Quaternion()
                {
                    eulerAngles = GetRandomVector3(-180, 180),
                }),
                InvokeOnlyOnClientId = m_AuthorityNetworkManager.LocalClientId,
                ReferenceTeleportHelperId = parent1.GetComponent<ReferenceRpcHelper>().NetworkObjectId,
            };

            SpawnSequenceController.AddAction(parentSequence1);
            SpawnSequenceController.AddAction(teleportSequence);
        }

        /// <summary>
        /// Test-8:
        /// Authority-> Spawn with no observers, change parent(1), NetworkShow, change ownership, Teleport RPC with NetworkBehaviourReference
        /// ClientOwner-> Teleport RPC using NetworkBehaviourReference
        /// </summary>
        private void ConfigureSequencesTest8(NetworkObject parent1)
        {
            SpawnSequenceController.CurrentTest = "Test8";

            // Authority
            var parentSequence1 = new ParentSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent1,
            };

            var networkShow = new NetworkShowSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn
            };

            var changeOwnership = new ChangeOwnershipSequence()
            {
                TargetOwnerClientId = GetNonAuthorityNetworkManager().LocalClientId,
                Stage = SpawnSequence.SpawnStage.AfterSpawn
            };

            var teleportSequence = new ReferenceRpcSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TeleportContext = TeleportSequence.TeleportContexts.OwnerTelportRpc,
                Position = GetRandomVector3(-10, 10),
                Rotation = (new Quaternion()
                {
                    eulerAngles = GetRandomVector3(-180, 180),
                }),
                InvokeOnlyOnClientId = m_AuthorityNetworkManager.LocalClientId,
                ReferenceTeleportHelperId = parent1.GetComponent<ReferenceRpcHelper>().NetworkObjectId,
            };

            SpawnSequenceController.AddAction(parentSequence1);
            SpawnSequenceController.AddAction(networkShow);
            SpawnSequenceController.AddAction(changeOwnership);
            SpawnSequenceController.AddAction(teleportSequence);
        }

        /// <summary>
        /// Test-9:
        /// Authority-> Spawn with no observers, NetworkShow, change parent(1)
        /// ClientOwner-> Teleport RPC using NetworkBehaviourReference
        /// </summary>
        private void ConfigureSequencesTest9(NetworkObject parent1)
        {
            SpawnSequenceController.CurrentTest = "Test9";

            // Authority

            var networkShow = new NetworkShowSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn
            };

            var parentSequence1 = new ParentSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent1,
            };

            SpawnSequenceController.AddAction(networkShow);
            SpawnSequenceController.AddAction(parentSequence1);
        }

        /// <summary>
        /// Test-10: (Client-Server Only)
        /// Authority-> Spawn with ownership, change parent (1), Wait (1), Parent RPC with NetworkObjectReference
        /// ClientOwner-> Re-parent (2) RPC using NetworkObjectReference
        /// </summary>
        private void ConfigureSequencesTest10_ClientServerOnly(NetworkObject parent1, NetworkObject parent2)
        {
            SpawnSequenceController.CurrentTest = "Test10 (Client-Server Only)";
            if (m_AuthorityNetworkManager.DistributedAuthorityMode)
            {
                SpawnSequenceController.ClientServerOnly = true;
                return;
            }

            // Authority
            var parentSequence1 = new ParentSequence()
            {
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TargetParent = parent1,
            };

            var parentRpc = new ReferenceRpcSequence()
            {
                IsParentRPC = true,
                Parent = parent2,
                ReferenceTeleportHelperId = parent1.GetComponent<ReferenceRpcHelper>().NetworkObjectId,
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
                TimeDelayInMS = 1000,
            };

            SpawnSequenceController.AddAction(parentSequence1);
            SpawnSequenceController.AddAction(parentRpc);
        }

        /// <summary>
        /// Test-11: (Client-Server Only)
        /// Authority-> Spawn with ownership, Parent RPC with NetworkObjectReference.
        /// ClientOwner-> Parent (1) RPC using NetworkObjectReference
        /// Server-> On the parent changing --> re-parent (2)
        /// </summary>
        private void ConfigureSequencesTest11_ClientServerOnly(NetworkObject parent1, NetworkObject parent2)
        {
            SpawnSequenceController.CurrentTest = "Test11 (Client-Server Only)";
            if (m_AuthorityNetworkManager.DistributedAuthorityMode)
            {
                SpawnSequenceController.ClientServerOnly = true;
                return;
            }

            var parentRpc = new ReferenceRpcSequence()
            {
                IsParentRPC = true,
                Parent = parent1,
                ReferenceTeleportHelperId = parent1.GetComponent<ReferenceRpcHelper>().NetworkObjectId,
                Stage = SpawnSequence.SpawnStage.AfterSpawn,
            };

            var reparent = new ParentSequence()
            {
                TargetParent = parent2,
                Stage = SpawnSequence.SpawnStage.Conditional,
            };

            // Authority
            var conditionalParent = new ConditionalParentSequence()
            {
                ConditionalSequence = reparent,
                ParentToWaitFor = parent1,
                Stage = SpawnSequence.SpawnStage.Conditional,
            };

            SpawnSequenceController.AddAction(parentRpc);
            SpawnSequenceController.AddAction(reparent);
            SpawnSequenceController.AddAction(conditionalParent);
        }
        #endregion

        #region Sequence Class Definitions

        internal class ConditionalParentSequence : SpawnSequence
        {
            public NetworkObject ParentToWaitFor;

            protected override bool OnShouldInvoke(SpawnStage stage)
            {
                // This could use additional properties to extend who
                // registers for the parenting event (i.e. in a client-server topology).
                if (m_NetworkObject.HasAuthority && stage == SpawnStage.PostSpawn)
                {
                    m_SpawnSequenceController.OnParentChanged += OnParentChanged;
                }
                return base.OnShouldInvoke(stage) && m_NetworkObject.HasAuthority;
            }

            private void OnParentChanged(NetworkObject parent)
            {
                if (ParentToWaitFor.NetworkObjectId == parent.NetworkObjectId)
                {
                    ConditionReached();
                }
            }
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
                    foreach (var clientId in m_NetworkObject.NetworkManager.ConnectedClientsIds)
                    {
                        if (clientId != m_NetworkObject.OwnerClientId)
                        {
                            Clients.Add(clientId);
                        }
                    }
                }

                foreach (var clientId in Clients)
                {
                    m_NetworkObject.NetworkShow(clientId);
                }

                base.OnAction();
            }
        }

        internal class ReferenceRpcSequence : TeleportSequence
        {
            public bool IsParentRPC;
            public ulong ReferenceTeleportHelperId;
            public NetworkObject Parent;

            protected override bool OnShouldInvoke(SpawnStage stage)
            {
                if (!m_NetworkObject.NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(ReferenceTeleportHelperId))
                {
                    Debug.LogError($"[Client-{m_NetworkObject.NetworkManager.LocalClientId}] NetworkObject-{ReferenceTeleportHelperId} is not spawned on this client!");
                }
                // Check if it is the right stage and if the instance can teleport
                return base.OnShouldInvoke(stage);
            }

            // We use the same teleport RPC logic to determine if we can invoke the action and
            // just do a broadcast RPC on an already spawned object. The primary validation here
            // is to assure message ordering is maintained so if you do use a reference
            // (NetworkObject or NetworkBehaviour) the target object will be spawned.
            // !!! This pattern cannot be used when spawning without observers and then doing a show followed by this kind of RPC !!!
            protected override void OnAction()
            {
                var referencRpcHelper = m_NetworkObject.NetworkManager.SpawnManager.SpawnedObjects[ReferenceTeleportHelperId].GetComponent<ReferenceRpcHelper>();
                if (!IsParentRPC)
                {
                    referencRpcHelper.ReferenceTeleportRpc(new NetworkBehaviourReference(m_SpawnSequenceController), Position, Rotation);
                }
                else
                {
                    referencRpcHelper.ReferenceParentRpc(new NetworkObjectReference(m_SpawnSequenceController.NetworkObject), new NetworkObjectReference(Parent));
                }
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
                return (m_NetworkObject.HasAuthority && TeleportContext == TeleportContexts.MotionAuthority) || (!m_NetworkObject.HasAuthority && TeleportContext == TeleportContexts.OwnerTelportRpc);
            }

            protected override bool OnShouldInvoke(SpawnStage stage)
            {
                // Check if it is the right stage and if the instance can teleport
                return base.OnShouldInvoke(stage) && CanTeleport();
            }

            protected override void OnAction()
            {
                if (m_OwnerTeleportRpc)
                {
                    m_SpawnSequenceController.TeleportRpc(Position, Rotation);
                }
                else
                {
                    m_SpawnSequenceController.SetState(Position, Rotation, teleportDisabled: false);
                }
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
                if (TargetOwnerClientId != m_NetworkObject.OwnerClientId)
                {
                    Debug.LogError($"[{m_NetworkObject.name}] Failed to change ownership!");
                }
                base.OnAction();
            }
        }

        internal class ParentSequence : SpawnSequence
        {
            public bool WorldPositionStays = true;
            public NetworkObject TargetParent;

            protected override bool OnShouldInvoke(SpawnStage stage)
            {
                // Don't invoke if the base says no
                if (!base.OnShouldInvoke(stage))
                {
                    return false;
                }

                // If sequence is configured to specifically invoke on this client
                if (InvokeOnlyOnClientId.HasValue && m_NetworkObject.NetworkManager.LocalClientId == InvokeOnlyOnClientId.Value)
                {
                    return true;
                }

                // Otherwise we should invoke if we have the authority to invoke
                return m_NetworkObject.HasAuthority || (m_NetworkObject.IsOwner && m_NetworkObject.AllowOwnerToParent);
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

        /// <summary>
        /// Derive from this to create a new type of spawn sequence
        /// or derive from an existing one to modify or extend the
        /// sequence's behavior.
        /// </summary>
        internal class SpawnSequence
        {
            public enum SpawnStage
            {
                Spawn,
                PostSpawn,
                AfterSpawn,
                Conditional,
            };

            public SpawnStage Stage;

            public SpawnSequence ConditionalSequence;

            public bool WasInvoked { get; protected set; }
            public bool InvokePending { get; protected set; }

            public int TimeDelayInMS = 0;

            public ulong? InvokeOnlyOnClientId;
            protected SpawnSequenceController m_SpawnSequenceController;
            protected NetworkObject m_NetworkObject;

            protected void ConditionReached()
            {
                if (ConditionalSequence != null)
                {
                    WasInvoked = true;
                    ConditionalSequence.Action(SpawnStage.Conditional, m_SpawnSequenceController);
                }
                else
                {
                    Debug.LogError($"[{GetType().Name}] Condition reached but {nameof(ConditionalSequence)} is null!");
                }
            }

            protected virtual bool OnShouldInvoke(SpawnStage stage)
            {
                if (InvokeOnlyOnClientId.HasValue && m_NetworkObject.NetworkManager.LocalClientId != InvokeOnlyOnClientId.Value)
                {
                    return false;
                }
                return Stage == stage;
            }

            protected virtual void OnAction()
            {

            }

            public void Action(SpawnStage stage, SpawnSequenceController spawnSequenceController)
            {
                m_NetworkObject = spawnSequenceController.NetworkObject;
                m_SpawnSequenceController = spawnSequenceController;
                if (OnShouldInvoke(stage) && !InvokePending)
                {
                    if (TimeDelayInMS == 0)
                    {
                        OnAction();
                        WasInvoked = true;
                    }
                    else
                    {
                        InvokePending = true;
                        m_SpawnSequenceController.StartCoroutine(TimeDelayCoroutine(stage, spawnSequenceController));
                    }
                }
            }

            private IEnumerator TimeDelayCoroutine(SpawnStage stage, SpawnSequenceController spawnSequenceController)
            {
                yield return new WaitForSeconds(TimeDelayInMS / 1000.0f);
                m_NetworkObject = spawnSequenceController.NetworkObject;
                m_SpawnSequenceController = spawnSequenceController;
                OnAction();
                WasInvoked = true;
                InvokePending = false;
            }
        }

        /// <summary>
        /// Process the current giveen set of configured spawn sequences. <br />
        /// <see cref="s_SpawnSequencedActions"/> contains the spawn sequences for a test configuraiton. <br />
        /// Some spawn sequences might only run under certain conditions determined within <see cref="ShouldRun(NetworkManager)"/>.
        /// </summary>
        public class SpawnSequenceController : NetworkTransform
        {
            public static bool VerboseLog;
            public static string CurrentTest;

            public static bool ClientServerOnly;
            public static bool DistributedAuthorityOnly;

            public static bool ShouldRun(NetworkManager authorityNetworkManager)
            {
                if (ClientServerOnly)
                {
                    return !authorityNetworkManager.DistributedAuthorityMode;
                }
                return true;
            }

            public void Log(string msg)
            {
                if (VerboseLog)
                {
                    Debug.Log($"[{name}] {msg}");
                }
            }

            // We can get away with using a static list since all instances share the same application domain
            // when running integration tests.
            private static List<SpawnSequence> s_SpawnSequencedActions = new List<SpawnSequence>();

            public static StringBuilder ErrorLog = new StringBuilder();

            public static void AddAction(SpawnSequence spawnSequence)
            {
                s_SpawnSequencedActions.Add(spawnSequence);
            }

            public static void Clear()
            {
                s_SpawnSequencedActions.Clear();
                ErrorLog.Clear();
                ClientServerOnly = false;
            }

            public static bool ActionIsPending()
            {
                foreach (var sequence in s_SpawnSequencedActions)
                {
                    if (sequence.InvokePending)
                    {
                        return true;
                    }
                }
                return false;
            }

            public static bool AllActionsInvoked()
            {
                ErrorLog.Clear();
                foreach (var sequence in s_SpawnSequencedActions)
                {
                    if (!sequence.WasInvoked)
                    {
                        ErrorLog.AppendLine($"[{sequence.GetType().Name}] Has not been invoked!");
                        return false;
                    }
                }
                return true;
            }

            public event System.Action<NetworkObject> OnParentChanged;

            public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
            {
                OnParentChanged?.Invoke(parentNetworkObject);
                base.OnNetworkObjectParentChanged(parentNetworkObject);
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
                Log($"[{nameof(OnNetworkSpawn)}] Invoked on client-{NetworkManager.LocalClientId}.");
                // Must invoke base first in order for CanCommit
                base.OnNetworkSpawn();
                InvokeSequencesForStage(SpawnSequence.SpawnStage.Spawn);
            }

            protected override void OnNetworkPostSpawn()
            {
                Log($"[{nameof(OnNetworkPostSpawn)}] Invoked on client-{NetworkManager.LocalClientId}.");
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
                Log($"[{nameof(TeleportRpc)}] Invoked on client-{NetworkManager.LocalClientId}.");
                SetState(posIn: position, rotIn: rotation, teleportDisabled: false);
            }
        }

        /// <summary>
        /// This is added to the generic spawned objects (parents) to validate that
        /// after having spawned a NetworkObject, as the authority, and then invoking
        /// an RPC, that accepts an <see cref="NetworkBehaviourReference"/> or <see cref="NetworkObjectReference"/>
        /// which references the newly spawned <see cref="NetworkObject"/> or an associated <see cref="NetworkBehaviour"/>
        /// component on an already known spawned object, that the object will have been spawned prior to the
        /// RPC being invoked on the non-authority side.
        /// </summary>
        /// <remarks>
        /// Currently, NGO does not support this usage pattern if you:
        /// - spawn with no observers
        /// - invoke the RPC with a reference to the spawned object within the same frame/call-stack
        /// This limitation is due to the network show defers the queuing of the <see cref="CreateObjectMessage"/>
        /// until the end of the frame as opposed to generating it when spawned. This could be supported if
        /// we convert to more of a command based system that is applied locally on the spawn authority side
        /// but queued and then messages are generated from the queued commands at the end of the frame.
        /// </remarks>
        public class ReferenceRpcHelper : NetworkBehaviour
        {
            [Rpc(SendTo.NotMe)]
            public void ReferenceTeleportRpc(NetworkBehaviourReference networkBehaviourReference, Vector3 position, Quaternion rotation, RpcParams rpcParams = default)
            {
                networkBehaviourReference.TryGet<SpawnSequenceController>(out var spawnSequenceController);
                if (spawnSequenceController != null)
                {
                    // This validation assumes that user script will broadcast an update to everyone but only the motion authority will apply the update.
                    if (spawnSequenceController.CanCommitToTransform)
                    {
                        spawnSequenceController.Log($"[{nameof(ReferenceRpcHelper)}][{nameof(ReferenceTeleportRpc)}] Invoked on client-{NetworkManager.LocalClientId}.");
                        spawnSequenceController.SetState(posIn: position, rotIn: rotation, teleportDisabled: false);
                    }
                }
                else
                {
                    Debug.LogError($"[{nameof(ReferenceTeleportRpc)}] Failed to resolve {nameof(NetworkBehaviourReference)}!");
                }
            }

            [Rpc(SendTo.Owner)]
            public void ReferenceParentRpc(NetworkObjectReference targetChildReference, NetworkObjectReference targetParentReference, RpcParams rpcParams = default)
            {
                targetChildReference.TryGet(out var networkObjectChild);
                if (networkObjectChild == null)
                {
                    Debug.LogError($"[{nameof(ReferenceParentRpc)}] Failed to resolve {nameof(NetworkObjectReference)} for {nameof(targetChildReference)}!");
                    return;
                }
                targetParentReference.TryGet(out var networkObjectParent);
                if (networkObjectParent == null)
                {
                    Debug.LogError($"[{nameof(ReferenceParentRpc)}] Failed to resolve {nameof(NetworkObjectReference)} for {nameof(targetParentReference)}!");
                    return;
                }
                var spawnSequenceController = networkObjectChild.GetComponent<SpawnSequenceController>();
                spawnSequenceController.Log($"[{nameof(ReferenceRpcHelper)}][{nameof(ReferenceTeleportRpc)}] Invoked on client-{NetworkManager.LocalClientId}.");

                networkObjectChild.TrySetParent(networkObjectParent);
            }
        }
        #endregion
    }
}
