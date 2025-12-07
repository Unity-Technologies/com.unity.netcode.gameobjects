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
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    [TestFixture(HostOrServer.DAHost)]
    internal class ComponentControllerTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private StringBuilder m_ErrorLog = new StringBuilder();
        private GameObject m_TestPrefab;

        private NetworkManager m_Authority;
        private ComponentController m_AuthorityController;

        public ComponentControllerTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override IEnumerator OnSetup()
        {
            m_ErrorLog.Clear();
            yield return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            // The source prefab contains the nested NetworkBehaviour that
            // will be parented under the target prefab.
            m_TestPrefab = CreateNetworkObjectPrefab("TestObject");
            var sourceChild = new GameObject("Child");
            sourceChild.transform.parent = m_TestPrefab.transform;
            var meshRenderer = sourceChild.AddComponent<MeshRenderer>();
            var light = sourceChild.AddComponent<Light>();
            var controller = m_TestPrefab.AddComponent<ComponentController>();
            controller.Components = new List<ComponentController.ComponentEntry>
            {
                new ComponentController.ComponentEntry()
                {
                    Component = meshRenderer,
                },
                new ComponentController.ComponentEntry()
                {
                    InvertEnabled = true,
                    Component = light,
                }
            };
            base.OnServerAndClientsCreated();
        }

        private bool AllClientsSpawnedInstances()
        {
            m_ErrorLog.Clear();
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_AuthorityController.NetworkObjectId))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Has not spawned {m_AuthorityController.name} yet!");
                }
            }
            return m_ErrorLog.Length == 0;
        }

        private void ControllerStateMatches(ComponentController controller)
        {
            if (m_AuthorityController.EnabledState != controller.EnabledState)
            {
                m_ErrorLog.AppendLine($"[Client-{controller.NetworkManager.LocalClientId}] The authority controller state ({m_AuthorityController.EnabledState})" +
                    $" does not match the local controller state ({controller.EnabledState})!");
                return;
            }

            if (m_AuthorityController.ValidComponents.Count != controller.ValidComponents.Count)
            {
                m_ErrorLog.AppendLine($"[Client-{controller.NetworkManager.LocalClientId}] The authority controller has {m_AuthorityController.ValidComponents.Count} valid components but " +
                    $"the local instance has {controller.ValidComponents.Count}!");
                return;
            }

            for (int i = 0; i < m_AuthorityController.ValidComponents.Count; i++)
            {
                var authorityEntry = m_AuthorityController.ValidComponents[i];
                var nonAuthorityEntry = controller.ValidComponents[i];
                if (authorityEntry.InvertEnabled != nonAuthorityEntry.InvertEnabled)
                {
                    m_ErrorLog.AppendLine($"[Client-{controller.NetworkManager.LocalClientId}] The authority controller's component entry ({i}) " +
                        $"has an inverted state of {authorityEntry.InvertEnabled} but the local instance has a value of " +
                        $"{nonAuthorityEntry.InvertEnabled}!");
                }

                var authorityIsEnabled = (bool)authorityEntry.PropertyInfo.GetValue(authorityEntry.Component);
                var nonAuthorityIsEnabled = (bool)nonAuthorityEntry.PropertyInfo.GetValue(authorityEntry.Component);
                if (authorityIsEnabled != nonAuthorityIsEnabled)
                {
                    m_ErrorLog.AppendLine($"[Client-{controller.NetworkManager.LocalClientId}] The authority controller's component ({i}) " +
                        $"entry's enabled state is {authorityIsEnabled} but the local instance's value is {nonAuthorityIsEnabled}!");
                }
            }
        }

        private bool AllComponentStatesMatch()
        {
            m_ErrorLog.Clear();
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_AuthorityController.NetworkObjectId))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Does not have a spawned instance of {m_AuthorityController.name}!");
                }
                var controller = networkManager.SpawnManager.SpawnedObjects[m_AuthorityController.NetworkObjectId].GetComponent<ComponentController>();
                ControllerStateMatches(controller);
            }
            return m_ErrorLog.Length == 0;
        }

        private bool AllComponentStatesAreCorrect(bool isEnabled)
        {
            m_ErrorLog.Clear();
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_AuthorityController.NetworkObjectId))
                {
                    m_ErrorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Does not have a spawned instance of {m_AuthorityController.name}!");
                }
                var controller = networkManager.SpawnManager.SpawnedObjects[m_AuthorityController.NetworkObjectId].GetComponent<ComponentController>();
                for (int i = 0; i < controller.ValidComponents.Count; i++)
                {
                    var componentEntry = controller.ValidComponents[i];

                    var componentEntryIsEnabled = (bool)componentEntry.PropertyInfo.GetValue(componentEntry.Component);
                    var valueToCheck = componentEntry.InvertEnabled ? !isEnabled : isEnabled;

                    if (valueToCheck != componentEntryIsEnabled)
                    {
                        m_ErrorLog.AppendLine($"[Client-{controller.NetworkManager.LocalClientId}] The enabled state for entry ({i}) " +
                            $"should be {valueToCheck} but is {componentEntryIsEnabled}!");
                    }
                }
            }
            return m_ErrorLog.Length == 0;
        }

        [UnityTest]
        public IEnumerator EnabledDisabledSynchronizationTests()
        {
            m_Authority = GetAuthorityNetworkManager();

            m_AuthorityController = SpawnObject(m_TestPrefab, m_Authority).GetComponent<ComponentController>();

            yield return WaitForConditionOrTimeOut(AllClientsSpawnedInstances);
            AssertOnTimeout($"All clients did not spawn an instance of {m_AuthorityController.name}!\n {m_ErrorLog}");

            // Validate that clients start off with matching states.
            yield return WaitForConditionOrTimeOut(AllComponentStatesMatch);
            AssertOnTimeout($"Not all client instances matched the authority instance {m_AuthorityController.name}! \n {m_ErrorLog}");

            // Validate that all controllers have the correct enabled value for the current authority controller instance's value.
            yield return WaitForConditionOrTimeOut(() => AllComponentStatesAreCorrect(m_AuthorityController.EnabledState));
            AssertOnTimeout($"Not all client instances have the correct enabled state!\n {m_ErrorLog}");

            // Toggle the enabled state of the authority controller
            m_AuthorityController.SetEnabled(!m_AuthorityController.EnabledState);

            // Validate that all controllers' states match
            yield return WaitForConditionOrTimeOut(AllComponentStatesMatch);
            AssertOnTimeout($"Not all client instances matched the authority instance {m_AuthorityController.name}! \n {m_ErrorLog}");

            // Validate that all controllers have the correct enabled value for the current authority controller instance's value.
            yield return WaitForConditionOrTimeOut(() => AllComponentStatesAreCorrect(m_AuthorityController.EnabledState));
            AssertOnTimeout($"Not all client instances have the correct enabled state!\n {m_ErrorLog}");

            // Late join a client to assure the late joining client's values are synchronized properly
            yield return CreateAndStartNewClient();

            // Validate that all controllers' states match
            yield return WaitForConditionOrTimeOut(AllComponentStatesMatch);
            AssertOnTimeout($"Not all client instances matched the authority instance {m_AuthorityController.name}! \n {m_ErrorLog}");

            // Validate that all controllers have the correct enabled value for the current authority controller instance's value.
            yield return WaitForConditionOrTimeOut(() => AllComponentStatesAreCorrect(m_AuthorityController.EnabledState));
            AssertOnTimeout($"Not all client instances have the correct enabled state!\n {m_ErrorLog}");
        }
    }
}
