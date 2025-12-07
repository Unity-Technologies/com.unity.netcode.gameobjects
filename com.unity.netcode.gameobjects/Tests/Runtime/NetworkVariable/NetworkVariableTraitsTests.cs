using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class NetworkVariableTraitsComponent : NetworkBehaviour
    {
        public NetworkVariable<float> TheVariable = new NetworkVariable<float>();
        public NetworkVariable<float> AnotherVariable = new NetworkVariable<float>();
    }

    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkVariableTraitsTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 3;

        private StringBuilder m_ErrorLog = new StringBuilder();

        public NetworkVariableTraitsTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnPlayerPrefabGameObjectCreated()
        {
            m_PlayerPrefab.AddComponent<NetworkVariableTraitsComponent>();
        }

        public NetworkVariableTraitsComponent GetAuthorityComponent()
        {
            return GetAuthorityNetworkManager().LocalClient.PlayerObject.GetComponent<NetworkVariableTraitsComponent>();
        }

        private bool AllAuthorityInstanceValuesMatch(float firstValue, float secondValue = 0.0f)
        {
            m_ErrorLog.Clear();
            var authorityComponent = GetAuthorityComponent();
            if (authorityComponent.TheVariable.Value != firstValue || authorityComponent.AnotherVariable.Value != secondValue)
            {
                m_ErrorLog.Append($"[Client-{authorityComponent.OwnerClientId}][{authorityComponent.name}] Authority values did not match ({firstValue} | {secondValue})! " +
                    $"TheVariable: {authorityComponent.TheVariable.Value} | AnotherVariable: {authorityComponent.AnotherVariable.Value}");
                return false;
            }
            foreach (var client in m_ClientNetworkManagers)
            {
                if (client.LocalClient.IsSessionOwner)
                {
                    continue;
                }
                if (!client.SpawnManager.SpawnedObjects.ContainsKey(authorityComponent.NetworkObjectId))
                {
                    m_ErrorLog.Append($"Failed to find {authorityComponent.name} instance on Client-{client.LocalClientId}!");
                    return false;
                }
                var testComponent = client.SpawnManager.SpawnedObjects[authorityComponent.NetworkObjectId].GetComponent<NetworkVariableTraitsComponent>();
                if (testComponent.TheVariable.Value != firstValue || testComponent.AnotherVariable.Value != secondValue)
                {
                    m_ErrorLog.Append($"[Client-{client.LocalClientId}][{testComponent.name}] Authority values did not match ({firstValue} | {secondValue})! " +
                        $"TheVariable: {testComponent.TheVariable.Value} | AnotherVariable: {testComponent.AnotherVariable.Value}");
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator WhenNewValueIsLessThanThreshold_VariableIsNotSerialized()
        {
            var authorityComponent = GetAuthorityComponent();
            authorityComponent.TheVariable.CheckExceedsDirtinessThreshold = (in float value, in float newValue) => Mathf.Abs(newValue - value) >= 0.1;

            var timeoutHelper = new TimeoutHelper(1.0f);
            var newValue = 0.05f;
            authorityComponent.TheVariable.Value = newValue;
            yield return WaitForConditionOrTimeOut(() => AllAuthorityInstanceValuesMatch(newValue), timeoutHelper);
            Assert.True(timeoutHelper.TimedOut, $"Non-authority instances recieved changes when they should not have!");
        }

        [UnityTest]
        public IEnumerator WhenNewValueIsGreaterThanThreshold_VariableIsSerialized()
        {
            var authorityComponent = GetAuthorityComponent();
            authorityComponent.TheVariable.CheckExceedsDirtinessThreshold = (in float value, in float newValue) => Mathf.Abs(newValue - value) >= 0.1;

            var timeoutHelper = new TimeoutHelper(1.0f);
            var newValue = 0.15f;
            authorityComponent.TheVariable.Value = newValue;
            yield return WaitForConditionOrTimeOut(() => AllAuthorityInstanceValuesMatch(newValue), timeoutHelper);
            AssertOnTimeout($"{m_ErrorLog}", timeoutHelper);
        }

        [UnityTest]
        public IEnumerator WhenNewValueIsLessThanThresholdButMaxTimeHasPassed_VariableIsSerialized()
        {
            var authorityComponent = GetAuthorityComponent();
            authorityComponent.TheVariable.CheckExceedsDirtinessThreshold = (in float value, in float newValue) => Mathf.Abs(newValue - value) >= 0.1;
            authorityComponent.TheVariable.SetUpdateTraits(new NetworkVariableUpdateTraits { MaxSecondsBetweenUpdates = 1.0f });
            authorityComponent.TheVariable.LastUpdateSent = authorityComponent.NetworkManager.NetworkTimeSystem.LocalTime;

            var timeoutHelper = new TimeoutHelper(0.62f);
            var newValue = 0.05f;
            authorityComponent.TheVariable.Value = newValue;
            // We expect a timeout for this condition
            yield return WaitForConditionOrTimeOut(() => AllAuthorityInstanceValuesMatch(newValue), timeoutHelper);
            Assert.True(timeoutHelper.TimedOut, $"Non-authority instances recieved changes when they should not have!");

            // Now we expect this to not timeout
            yield return WaitForConditionOrTimeOut(() => AllAuthorityInstanceValuesMatch(newValue), timeoutHelper);
            AssertOnTimeout($"{m_ErrorLog}", timeoutHelper);
        }

        [UnityTest]
        public IEnumerator WhenNewValueIsGreaterThanThresholdButMinTimeHasNotPassed_VariableIsNotSerialized()
        {
            var authorityComponent = GetAuthorityComponent();
            authorityComponent.TheVariable.CheckExceedsDirtinessThreshold = (in float value, in float newValue) => Mathf.Abs(newValue - value) >= 0.1;
            authorityComponent.TheVariable.SetUpdateTraits(new NetworkVariableUpdateTraits { MinSecondsBetweenUpdates = 1 });
            authorityComponent.TheVariable.LastUpdateSent = authorityComponent.NetworkManager.NetworkTimeSystem.LocalTime;

            var timeoutHelper = new TimeoutHelper(0.62f);
            var newValue = 0.15f;
            authorityComponent.TheVariable.Value = newValue;
            // We expect a timeout for this condition
            yield return WaitForConditionOrTimeOut(() => AllAuthorityInstanceValuesMatch(newValue), timeoutHelper);
            Assert.True(timeoutHelper.TimedOut, $"Non-authority instances recieved changes when they should not have!");

            // Now we expect this to not timeout
            yield return WaitForConditionOrTimeOut(() => AllAuthorityInstanceValuesMatch(newValue), timeoutHelper);
            AssertOnTimeout($"{m_ErrorLog}", timeoutHelper);
        }

        [UnityTest]
        public IEnumerator WhenNoThresholdIsSetButMinTimeHasNotPassed_VariableIsNotSerialized()
        {
            var authorityComponent = GetAuthorityComponent();
            authorityComponent.TheVariable.SetUpdateTraits(new NetworkVariableUpdateTraits { MinSecondsBetweenUpdates = 1 });
            authorityComponent.TheVariable.LastUpdateSent = authorityComponent.NetworkManager.NetworkTimeSystem.LocalTime;

            var timeoutHelper = new TimeoutHelper(0.62f);
            var newValue = 0.15f;
            authorityComponent.TheVariable.Value = newValue;
            // We expect a timeout for this condition
            yield return WaitForConditionOrTimeOut(() => AllAuthorityInstanceValuesMatch(newValue), timeoutHelper);
            Assert.True(timeoutHelper.TimedOut, $"Non-authority instances recieved changes when they should not have!");

            // Now we expect this to not timeout
            yield return WaitForConditionOrTimeOut(() => AllAuthorityInstanceValuesMatch(newValue), timeoutHelper);
            AssertOnTimeout($"{m_ErrorLog}", timeoutHelper);
        }

        /// <summary>
        /// Integration test to validate that a <see cref="NetworkVariable{T}"/> with <see cref="NetworkVariableUpdateTraits"/>
        /// does not cause other <see cref="NetworkVariable{T}"/>s to miss an update when they are dirty but the one with
        /// traits is not ready to send an update.
        /// </summary>
        [UnityTest]
        public IEnumerator WhenNonTraitsIsDirtyButTraitsIsNotReadyToSend()
        {
            var authorityComponent = GetAuthorityComponent();
            authorityComponent.TheVariable.SetUpdateTraits(new NetworkVariableUpdateTraits { MinSecondsBetweenUpdates = 1 });
            authorityComponent.TheVariable.LastUpdateSent = authorityComponent.NetworkManager.NetworkTimeSystem.LocalTime;

            var timeoutHelper = new TimeoutHelper(0.62f);
            var firstValue = 0.15f;
            var secondValue = 0.15f;
            authorityComponent.TheVariable.Value = firstValue;
            // We expect a timeout for this condition
            yield return WaitForConditionOrTimeOut(() => AllAuthorityInstanceValuesMatch(firstValue, secondValue), timeoutHelper);
            Assert.True(timeoutHelper.TimedOut, $"Non-authority instances recieved changes when they should not have!");

            secondValue = 1.5f;
            authorityComponent.AnotherVariable.Value = secondValue;
            // Now we expect this to not timeout
            yield return WaitForConditionOrTimeOut(() => AllAuthorityInstanceValuesMatch(firstValue, secondValue), timeoutHelper);
            AssertOnTimeout($"{m_ErrorLog}", timeoutHelper);
        }
    }
}
