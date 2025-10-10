using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class OwnershipPermissionsTests : IntegrationTestWithApproximation
    {
        private GameObject m_PermissionsObject;

        private StringBuilder m_ErrorLog = new StringBuilder();

        protected override int NumberOfClients => 4;

        public OwnershipPermissionsTests() : base(HostOrServer.DAHost)
        {
        }

        protected override IEnumerator OnSetup()
        {
            m_ObjectToValidate = null;
            OwnershipPermissionsTestHelper.CurrentOwnedInstance = null;
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PermissionsObject = CreateNetworkObjectPrefab("PermObject");
            m_PermissionsObject.AddComponent<OwnershipPermissionsTestHelper>();

            base.OnServerAndClientsCreated();
        }

        private NetworkObject m_ObjectToValidate;


        private bool ValidatePermissionsOnAllClients(StringBuilder errorLog)
        {
            var currentPermissions = (ushort)m_ObjectToValidate.Ownership;
            var networkObjectId = m_ObjectToValidate.NetworkObjectId;
            var objectName = m_ObjectToValidate.name;

            foreach (var client in m_NetworkManagers)
            {
                var otherPermissions = (ushort)client.SpawnManager.SpawnedObjects[networkObjectId].Ownership;
                if (currentPermissions != otherPermissions)
                {
                    errorLog.Append($"Client-{client.LocalClientId} permissions for {objectName} is {otherPermissions} when it should be {currentPermissions}!");
                    return false;
                }
            }
            return true;
        }

        private bool WaitForOneClientToBeApproved(OwnershipPermissionsTestHelper[] clients)
        {
            var approvedClients = 0;
            var requestInProgressClients = 0;
            foreach (var helper in clients)
            {
                if (helper.OwnershipRequestResponseStatus == NetworkObject.OwnershipRequestResponseStatus.Approved)
                {
                    approvedClients++;
                }
                else if (helper.OwnershipRequestResponseStatus == NetworkObject.OwnershipRequestResponseStatus.RequestInProgress)
                {
                    requestInProgressClients++;
                }
            }

            return approvedClients == 1 && requestInProgressClients == clients.Length - 1;
        }

        private bool ValidateAllInstancesAreOwnedByClient(ulong clientId)
        {
            m_ErrorLog.Clear();

            var networkObjectId = m_ObjectToValidate.NetworkObjectId;
            foreach (var client in m_NetworkManagers)
            {
                var otherNetworkObject = client.SpawnManager.SpawnedObjects[networkObjectId];
                if (otherNetworkObject.OwnerClientId != clientId)
                {
                    m_ErrorLog.Append($"[Client-{client.LocalClientId}][{otherNetworkObject.name}] Expected owner to be {clientId} but it was {otherNetworkObject.OwnerClientId}!");
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator ValidateOwnershipPermissionsTest()
        {
            var firstClient = GetNonAuthorityNetworkManager(0);
            var firstInstance = SpawnObject(m_PermissionsObject, firstClient).GetComponent<NetworkObject>();
            OwnershipPermissionsTestHelper.CurrentOwnedInstance = firstInstance;
            var firstInstanceHelper = firstInstance.GetComponent<OwnershipPermissionsTestHelper>();
            var networkObjectId = firstInstance.NetworkObjectId;
            m_ObjectToValidate = OwnershipPermissionsTestHelper.CurrentOwnedInstance;

            yield return WaitForSpawnedOnAllOrTimeOut(firstInstance);
            AssertOnTimeout($"[Failed To Spawn] Ownership permissions object {firstInstance.name} failed to spawn!");

            // Validate the base non-assigned permissions value for all instances are the same.
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[Permissions Mismatch] {firstInstance.name} has incorrect ownership permissions!");

            //////////////////////////////////////
            // Setting & Removing Ownership Flags:
            //////////////////////////////////////

            // Now, cycle through all permissions and validate that when the owner changes them the change
            // is synchronized on all non-owner clients.
            foreach (var permissionObject in Enum.GetValues(typeof(NetworkObject.OwnershipStatus)))
            {
                var permission = (NetworkObject.OwnershipStatus)permissionObject;
                // Adding the SessionOwner flag here should fail as this NetworkObject is not owned by the Session Owner
                if (permission == NetworkObject.OwnershipStatus.SessionOwner)
                {
                    Assert.IsFalse(firstInstance.SetOwnershipStatus(permission), $"[Add][IncorrectPermissions] Setting {NetworkObject.OwnershipStatus.SessionOwner} is not valid when the client is not the Session Owner: \n {m_ErrorLog}!");
                    continue;
                }
                // Add the status
                firstInstance.SetOwnershipStatus(permission);
                // Validate the permissions value for all instances are the same.
                yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
                AssertOnTimeout($"[Add][Permissions Mismatch] {firstInstance.name}");

                // Remove the status unless it is None (ignore None).
                if (permission == NetworkObject.OwnershipStatus.None)
                {
                    continue;
                }
                firstInstance.RemoveOwnershipStatus(permission);
                // Validate the permissions value for all instances are the same.
                yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
                AssertOnTimeout($"[Remove][Permissions Mismatch] {firstInstance.name}");
            }

            //Add multiple flags at the same time
            var multipleFlags = NetworkObject.OwnershipStatus.Transferable | NetworkObject.OwnershipStatus.Distributable | NetworkObject.OwnershipStatus.RequestRequired;
            firstInstance.SetOwnershipStatus(multipleFlags, true);
            Assert.IsTrue(firstInstance.HasOwnershipStatus(multipleFlags), $"[Set][Multi-flag Failure] Expected: {(ushort)multipleFlags} but was {(ushort)firstInstance.Ownership}!");

            // Validate the permissions value for all instances are the same.
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[Set Multiple][Permissions Mismatch] {firstInstance.name}");

            // Remove multiple flags at the same time
            multipleFlags = NetworkObject.OwnershipStatus.Transferable | NetworkObject.OwnershipStatus.RequestRequired;
            firstInstance.RemoveOwnershipStatus(multipleFlags);
            // Validate the two flags no longer are set
            Assert.IsFalse(firstInstance.HasOwnershipStatus(multipleFlags), $"[Remove][Multi-flag Failure] Expected: {(ushort)NetworkObject.OwnershipStatus.Distributable} but was {(ushort)firstInstance.Ownership}!");
            // Validate that the Distributable flag is still set
            Assert.IsTrue(firstInstance.HasOwnershipStatus(NetworkObject.OwnershipStatus.Distributable), $"[Remove][Multi-flag Failure] Expected: {(ushort)NetworkObject.OwnershipStatus.Distributable} but was {(ushort)firstInstance.Ownership}!");

            // Validate the permissions value for all instances are the same.
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[Set Multiple][Permissions Mismatch] {firstInstance.name}");

            //////////////////////
            // Changing Ownership:
            //////////////////////

            // Clear the flags, set the permissions to transferrable, and lock ownership in one pass.
            firstInstance.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable, true, NetworkObject.OwnershipLockActions.SetAndLock);

            // Validate the permissions value for all instances are the same.
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[Reset][Permissions Mismatch] {firstInstance.name}");

            var secondClient = GetNonAuthorityNetworkManager(1);
            var secondInstance = secondClient.SpawnManager.SpawnedObjects[networkObjectId];
            var secondInstanceHelper = secondInstance.GetComponent<OwnershipPermissionsTestHelper>();

            secondInstance.ChangeOwnership(secondClient.LocalClientId);
            Assert.IsTrue(secondInstanceHelper.OwnershipPermissionsFailureStatus == NetworkObject.OwnershipPermissionsFailureStatus.Locked,
                $"Expected {secondInstance.name} to return {NetworkObject.OwnershipPermissionsFailureStatus.Locked} but its permission failure" +
                $" status is {secondInstanceHelper.OwnershipPermissionsFailureStatus}!");

            firstInstance.SetOwnershipLock(false);
            // Validate the permissions value for all instances are the same.
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[Unlock][Permissions Mismatch] {firstInstance.name}");

            // Sanity check to assure this client's instance isn't already the owner.
            Assert.True(!secondInstance.IsOwner, $"[Ownership Check] Client-{secondClient.LocalClientId} already is the owner!");

            // With transferable ownership, the second client shouldn't be able to request ownership
            var requestStatus = secondInstance.RequestOwnership();
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestRequiredNotSet, $"Client-{secondClient.LocalClientId} was unable to send a request for ownership because: {requestStatus}!");

            // Now try to acquire ownership
            secondInstance.ChangeOwnership(secondClient.LocalClientId);

            // Validate the permissions value for all instances are the same
            yield return WaitForConditionOrTimeOut(() => secondInstance.IsOwner);
            AssertOnTimeout($"[Acquire Ownership Failed] Client-{secondClient.LocalClientId} failed to get ownership!");

            m_ObjectToValidate = OwnershipPermissionsTestHelper.CurrentOwnedInstance;
            // Validate all other client instances are showing the same owner
            yield return WaitForConditionOrTimeOut(() => ValidateAllInstancesAreOwnedByClient(secondClient.LocalClientId));
            AssertOnTimeout($"[Ownership Mismatch] {secondInstance.name}: \n {m_ErrorLog}");

            // Clear the flags, set the permissions to RequestRequired, and lock ownership in one pass.
            secondInstance.SetOwnershipStatus(NetworkObject.OwnershipStatus.RequestRequired, true);

            // Validate the permissions value for all instances are the same.
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[Unlock][Permissions Mismatch] {secondInstance.name}");

            // Attempt to acquire ownership by just changing it
            firstInstance.ChangeOwnership(firstClient.LocalClientId);

            // Assure we are denied ownership due to it requiring ownership be requested
            Assert.IsTrue(firstInstanceHelper.OwnershipPermissionsFailureStatus == NetworkObject.OwnershipPermissionsFailureStatus.RequestRequired,
                $"Expected {secondInstance.name} to return {NetworkObject.OwnershipPermissionsFailureStatus.RequestRequired} but its permission failure" +
                $" status is {secondInstanceHelper.OwnershipPermissionsFailureStatus}!");

            //////////////////////////////////
            // Test for single race condition:
            //////////////////////////////////

            // Start with a request for the client we expect to be given ownership
            requestStatus = firstInstance.RequestOwnership();
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestSent, $"Client-{firstClient.LocalClientId} was unable to send a request for ownership because: {requestStatus}!");

            yield return null;

            // Get the 3rd client to send a request at the "relatively" same time
            var thirdClient = GetNonAuthorityNetworkManager(2);
            var thirdInstance = thirdClient.SpawnManager.SpawnedObjects[networkObjectId];
            var thirdInstanceHelper = thirdInstance.GetComponent<OwnershipPermissionsTestHelper>();

            // At the same time send a request by the third client.
            requestStatus = thirdInstance.RequestOwnership();

            // We expect the 3rd client's request should be able to be sent at this time as well (i.e. creates the race condition between two clients)
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestSent, $"Client-{thirdClient.LocalClientId} was unable to send a request for ownership because: {requestStatus}!");

            // We expect the first requesting client to be given ownership
            yield return WaitForConditionOrTimeOut(() => firstInstance.IsOwner);
            AssertOnTimeout($"[Acquire Ownership Failed] Client-{firstClient.LocalClientId} failed to get ownership! ({firstInstanceHelper.OwnershipRequestResponseStatus})(Owner: {OwnershipPermissionsTestHelper.CurrentOwnedInstance.OwnerClientId}");
            m_ObjectToValidate = OwnershipPermissionsTestHelper.CurrentOwnedInstance;

            // Just do a sanity check to assure ownership has changed on all clients.
            yield return WaitForConditionOrTimeOut(() => ValidateAllInstancesAreOwnedByClient(firstClient.LocalClientId));
            AssertOnTimeout($"[Ownership Mismatch] {firstInstance.name}: \n {m_ErrorLog}");

            // Now, the third client should get a RequestInProgress returned as their request response
            yield return WaitForConditionOrTimeOut(() => thirdInstanceHelper.OwnershipRequestResponseStatus == NetworkObject.OwnershipRequestResponseStatus.RequestInProgress);
            AssertOnTimeout($"[Request In Progress Failed] Client-{thirdClient.LocalClientId} did not get the right request denied response!");

            // Validate the permissions value for all instances are the same.
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[Unlock][Permissions Mismatch] {firstInstance.name}");

            // Check for various permissions denied race conditions with changing permissions and requesting ownership
            yield return ValidateRequestAndPermissionsChangeRaceCondition(NetworkObject.OwnershipStatus.Distributable, NetworkObject.OwnershipLockActions.SetAndUnlock, NetworkObject.OwnershipRequestResponseStatus.CannotRequest, firstClient, firstInstance, secondInstance, secondInstanceHelper);
            yield return ValidateRequestAndPermissionsChangeRaceCondition(NetworkObject.OwnershipStatus.Transferable, NetworkObject.OwnershipLockActions.SetAndLock, NetworkObject.OwnershipRequestResponseStatus.Locked, firstClient, firstInstance, secondInstance, secondInstanceHelper);

            // Should successfully change ownership to secondClient
            yield return ValidateRequestAndPermissionsChangeRaceCondition(NetworkObject.OwnershipStatus.Transferable, NetworkObject.OwnershipLockActions.SetAndUnlock, NetworkObject.OwnershipRequestResponseStatus.Approved, firstClient, firstInstance, secondInstance, secondInstanceHelper);

            // Transfer ownership back to firstClient. ValidateRequestAndPermissionsChangeRaceCondition will reset permissions to RequestRequired
            yield return ValidateRequestAndPermissionsChangeRaceCondition(NetworkObject.OwnershipStatus.Transferable, NetworkObject.OwnershipLockActions.None, NetworkObject.OwnershipRequestResponseStatus.Approved, secondClient, secondInstance, firstInstance, firstInstanceHelper, false);

            ///////////////////////////////////////////////
            // Test for multiple ownership race conditions:
            ///////////////////////////////////////////////

            // Get the 4th client's instance
            var fourthClient = GetNonAuthorityNetworkManager(3);
            var fourthInstance = fourthClient.SpawnManager.SpawnedObjects[networkObjectId];
            var fourthInstanceHelper = fourthInstance.GetComponent<OwnershipPermissionsTestHelper>();

            // Send out a request from three clients at the same time
            // The first one sent (and received for this test) gets ownership
            requestStatus = secondInstance.RequestOwnership();
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestSent, $"Client-{secondClient.LocalClientId} was unable to send a request for ownership because: {requestStatus}!");
            requestStatus = thirdInstance.RequestOwnership();
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestSent, $"Client-{thirdClient.LocalClientId} was unable to send a request for ownership because: {requestStatus}!");
            requestStatus = fourthInstance.RequestOwnership();
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestSent, $"Client-{fourthClient.LocalClientId} was unable to send a request for ownership because: {requestStatus}!");

            // The 2nd and 3rd client should be denied and the 4th client should be approved
            yield return WaitForConditionOrTimeOut(() => WaitForOneClientToBeApproved(new[] { secondInstanceHelper, thirdInstanceHelper, fourthInstanceHelper }));
            AssertOnTimeout("[Targeted Owner] A client received an incorrect response. " +
                            $"Expected one client to have {NetworkObject.OwnershipRequestResponseStatus.Approved} and the others to have {NetworkObject.OwnershipRequestResponseStatus.RequestInProgress}!."
                            + $"\n Client-{fourthClient.LocalClientId}: has {fourthInstanceHelper.OwnershipRequestResponseStatus}!"
                            + $"\n Client-{thirdClient.LocalClientId}: has {thirdInstanceHelper.OwnershipRequestResponseStatus}!"
                            + $"\n Client-{secondClient.LocalClientId}: has {secondInstanceHelper.OwnershipRequestResponseStatus}!");

            m_ObjectToValidate = OwnershipPermissionsTestHelper.CurrentOwnedInstance;
            // Just do a sanity check to assure ownership has changed on all clients.
            yield return WaitForConditionOrTimeOut(() => ValidateAllInstancesAreOwnedByClient(secondClient.LocalClientId));
            AssertOnTimeout($"[Multiple request race condition][Ownership Mismatch] {secondInstance.name}: \n {m_ErrorLog}");

            // Validate the permissions value for all instances are the same.
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[Multiple request race condition][Permissions Mismatch] {secondInstance.name}");

            ///////////////////////////////////////////////
            // Test for targeted ownership request:
            ///////////////////////////////////////////////

            // Now get the DAHost's client's instance
            var authority = GetAuthorityNetworkManager();
            var authorityInstance = authority.SpawnManager.SpawnedObjects[networkObjectId];
            var authorityInstanceHelper = authorityInstance.GetComponent<OwnershipPermissionsTestHelper>();

            secondInstanceHelper.AllowOwnershipRequest = true;
            secondInstanceHelper.OnlyAllowTargetClientId = true;
            secondInstanceHelper.ClientToAllowOwnership = authority.LocalClientId;

            // Send out a request from all three clients
            requestStatus = firstInstance.RequestOwnership();
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestSent, $"Client-{firstClient.LocalClientId} was unable to send a request for ownership because: {requestStatus}!");
            requestStatus = thirdInstance.RequestOwnership();
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestSent, $"Client-{thirdClient.LocalClientId} was unable to send a request for ownership because: {requestStatus}!");
            requestStatus = fourthInstance.RequestOwnership();
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestSent, $"Client-{fourthClient.LocalClientId} was unable to send a request for ownership because: {requestStatus}!");
            requestStatus = authorityInstance.RequestOwnership();
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestSent, $"Client-{authority.LocalClientId} was unable to send a request for ownership because: {requestStatus}!");

            // Only the client marked as ClientToAllowOwnership (daHost) should be approved. All others should be denied.
            yield return WaitForConditionOrTimeOut(() =>
            (firstInstanceHelper.OwnershipRequestResponseStatus == NetworkObject.OwnershipRequestResponseStatus.Denied) &&
            (thirdInstanceHelper.OwnershipRequestResponseStatus == NetworkObject.OwnershipRequestResponseStatus.Denied) &&
            (fourthInstanceHelper.OwnershipRequestResponseStatus == NetworkObject.OwnershipRequestResponseStatus.Denied) &&
            (authorityInstanceHelper.OwnershipRequestResponseStatus == NetworkObject.OwnershipRequestResponseStatus.Approved)
            );
            AssertOnTimeout($"[Targeted Owner] A client received an incorrect response."
                + $"\n Client-{firstClient.LocalClientId}: Expected {NetworkObject.OwnershipRequestResponseStatus.Denied}, and got {firstInstanceHelper.OwnershipRequestResponseStatus}!"
                + $"\n Client-{thirdClient.LocalClientId}: Expected {NetworkObject.OwnershipRequestResponseStatus.Denied}, and got {thirdInstanceHelper.OwnershipRequestResponseStatus}!"
                + $"\n Client-{fourthClient.LocalClientId}: Expected {NetworkObject.OwnershipRequestResponseStatus.Denied}, and got {fourthInstanceHelper.OwnershipRequestResponseStatus}!"
                + $"\n Client-{authority.LocalClientId}: Expected {NetworkObject.OwnershipRequestResponseStatus.Approved}, and got {authorityInstanceHelper.OwnershipRequestResponseStatus}!");

            ///////////////////////////////////////////////
            // Test OwnershipStatus.SessionOwner:
            ///////////////////////////////////////////////

            OwnershipPermissionsTestHelper.CurrentOwnedInstance = authorityInstance;
            m_ObjectToValidate = OwnershipPermissionsTestHelper.CurrentOwnedInstance;

            // Add multiple statuses
            authorityInstance.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable | NetworkObject.OwnershipStatus.SessionOwner);
            // Validate the permissions value for all instances are the same.
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[Add][Permissions Mismatch] {authorityInstance.name}");

            // Trying to set SessionOwner flag should override any other flags.
            Assert.IsFalse(authorityInstance.HasOwnershipStatus(NetworkObject.OwnershipStatus.Transferable), $"[Set][SessionOwner flag Failure] Expected: {NetworkObject.OwnershipStatus.Transferable} not to be set!");

            // Add another status. Should fail as SessionOwner should be exclusive
            authorityInstance.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable);
            Assert.IsFalse(authorityInstance.HasOwnershipStatus(NetworkObject.OwnershipStatus.Distributable), $"[Add][SessionOwner flag Failure] Expected: {NetworkObject.OwnershipStatus.Transferable} not to be set!");

            // Request ownership of the SessionOwner flag instance
            requestStatus = firstInstance.RequestOwnership();
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestRequiredNotSet, $"Client-{firstClient.LocalClientId} should not be able to send a request for ownership because object is marked as owned by the session owner. {requestStatus}!");

            // Set ownership directly on local object. This will allow the request to be sent
            firstInstance.Ownership = NetworkObject.OwnershipStatus.RequestRequired;
            requestStatus = firstInstance.RequestOwnership();
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestSent, $"Client-{firstClient.LocalClientId} was unable to send a request for ownership because: {requestStatus}!");

            // Request should be denied with CannotRequest
            yield return WaitForConditionOrTimeOut(() => firstInstanceHelper.OwnershipRequestResponseStatus == NetworkObject.OwnershipRequestResponseStatus.CannotRequest);
            AssertOnTimeout($"[Targeted Owner] Client-{firstClient.LocalClientId} did not get the right request response: {firstInstanceHelper.OwnershipRequestResponseStatus} Expecting: {NetworkObject.OwnershipRequestResponseStatus.CannotRequest}!");

            // Try changing the ownership explicitly
            // Get the cloned authorityClient instance on a client side
            var clientInstance = thirdClient.SpawnManager.SpawnedObjects[authorityInstance.NetworkObjectId];

            // Get the client instance of the OwnershipPermissionsTestHelper component
            var clientInstanceHelper = clientInstance.GetComponent<OwnershipPermissionsTestHelper>();

            // Have the client attempt to change ownership
            clientInstance.ChangeOwnership(thirdClient.LocalClientId);

            // Verify the client side gets a permission failure status of NetworkObject.OwnershipPermissionsFailureStatus.SessionOwnerOnly
            Assert.IsTrue(clientInstanceHelper.OwnershipPermissionsFailureStatus == NetworkObject.OwnershipPermissionsFailureStatus.SessionOwnerOnly,
                $"Expected {clientInstance.name} to return {NetworkObject.OwnershipPermissionsFailureStatus.SessionOwnerOnly} but its permission failure" +
                $" status is {clientInstanceHelper.OwnershipPermissionsFailureStatus}!");

            // Have the session owner attempt to change ownership to a non-session owner
            authorityInstance.ChangeOwnership(thirdClient.LocalClientId);

            // Verify the session owner cannot assign a SessionOwner permission NetworkObject to a non-authority client
            Assert.IsTrue(authorityInstanceHelper.OwnershipPermissionsFailureStatus == NetworkObject.OwnershipPermissionsFailureStatus.SessionOwnerOnly,
                $"Expected {authorityInstance.name} to return {NetworkObject.OwnershipPermissionsFailureStatus.SessionOwnerOnly} but its permission failure" +
                $" status is {authorityInstanceHelper.OwnershipPermissionsFailureStatus}!");

            // Remove status
            authorityInstance.RemoveOwnershipStatus(NetworkObject.OwnershipStatus.SessionOwner);
            // Validate the permissions value for all instances are the same.
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[Remove][Permissions Mismatch] {authorityInstance.name}");
        }


        [UnityTest]
        public IEnumerator ChangeOwnershipWithoutObservers()
        {
            var authority = GetAuthorityNetworkManager();
            var initialLogLevel = authority.LogLevel;
            authority.LogLevel = LogLevel.Developer;

            var authorityInstance = SpawnObject(m_PermissionsObject, authority).GetComponent<NetworkObject>();
            OwnershipPermissionsTestHelper.CurrentOwnedInstance = authorityInstance;
            m_ObjectToValidate = OwnershipPermissionsTestHelper.CurrentOwnedInstance;

            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance);
            AssertOnTimeout($"[Failed To Spawn] {authorityInstance.name}");

            authorityInstance.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable, true);
            // Validate the base non-assigned permissions value for all instances are the same.
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[Permissions Mismatch] {authorityInstance.name}");

            var otherClient = GetNonAuthorityNetworkManager(0);
            var otherInstance = otherClient.SpawnManager.SpawnedObjects[authorityInstance.NetworkObjectId];

            // Remove the client from the observers list
            authorityInstance.Observers.Remove(otherClient.LocalClientId);

            // ChangeOwnership should fail
            authorityInstance.ChangeOwnership(otherClient.LocalClientId);
            var senderId = authority.LocalClientId;
            var receiverId = otherClient.LocalClientId;
            LogAssert.Expect(LogType.Warning, $"[Session-Owner Sender={senderId}] [Invalid Owner] Cannot send Ownership change as client-{receiverId} cannot see {authorityInstance.name}! Use NetworkShow first.");
            Assert.True(authorityInstance.IsOwner, $"[Ownership Check] Client-{senderId} should still own this object!");

            // Now re-add the client to the Observers list and try to change ownership
            authorityInstance.Observers.Add(otherClient.LocalClientId);
            authorityInstance.ChangeOwnership(otherClient.LocalClientId);

            // Validate the non-authority client now owns the object
            yield return WaitForConditionOrTimeOut(() => otherInstance.IsOwner);
            AssertOnTimeout($"[Acquire Ownership Failed] Client-{otherClient.LocalClientId} failed to get ownership!");

            authority.LogLevel = initialLogLevel;
        }

        private IEnumerator ValidateRequestAndPermissionsChangeRaceCondition(NetworkObject.OwnershipStatus newStatus, NetworkObject.OwnershipLockActions lockActions, NetworkObject.OwnershipRequestResponseStatus expectedResponseStatus, NetworkManager firstClient, NetworkObject firstInstance, NetworkObject secondInstance, OwnershipPermissionsTestHelper secondInstanceHelper, bool setFlagsFirst = true)
        {
            var secondClientId = secondInstance.NetworkManager.LocalClientId;

            if (setFlagsFirst)
            {
                firstInstance.SetOwnershipStatus(newStatus, true, lockActions);
            }

            // Request ownership
            var requestStatus = secondInstance.RequestOwnership();

            if (!setFlagsFirst)
            {
                firstInstance.SetOwnershipStatus(newStatus, true, lockActions);

            }

            // We expect the request should be able to be sent at this time as well (i.e. creates the race condition between request and permissions change)
            Assert.True(requestStatus == NetworkObject.OwnershipRequestStatus.RequestSent, $"[{newStatus}] Client-{firstClient.LocalClientId} was unable to send a request for ownership because: {requestStatus}!");

            yield return WaitForConditionOrTimeOut(() => secondInstanceHelper.OwnershipRequestResponseStatus == expectedResponseStatus);
            AssertOnTimeout($"[{newStatus}][Request race condition failed] Client-{secondClientId} did not get the right request response status! Expected: {expectedResponseStatus} Received: {secondInstanceHelper.OwnershipRequestResponseStatus}!");

            var expectedOwner = expectedResponseStatus == NetworkObject.OwnershipRequestResponseStatus.Approved ? secondClientId : firstClient.LocalClientId;
            yield return WaitForConditionOrTimeOut(() => ValidateAllInstancesAreOwnedByClient(expectedOwner));
            AssertOnTimeout($"[{newStatus}][Request race condition][Ownership Mismatch] Expected Client-{expectedOwner} to have ownership: \n {m_ErrorLog}");

            // Owner permissions should prevail - ownership shouldn't change
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[{newStatus}][Unlock][Race condition permissions mismatch] {secondInstance.name}");

            // Reset the permissions to requestRequired
            var finalInstance = expectedResponseStatus == NetworkObject.OwnershipRequestResponseStatus.Approved ? secondInstance : firstInstance;

            finalInstance.SetOwnershipStatus(NetworkObject.OwnershipStatus.RequestRequired, true);
            yield return WaitForConditionOrTimeOut(ValidatePermissionsOnAllClients);
            AssertOnTimeout($"[{newStatus}][Set RequestRequired][Permissions mismatch] {firstClient.name}");
        }

        internal class OwnershipPermissionsTestHelper : NetworkBehaviour
        {
            public static NetworkObject CurrentOwnedInstance;

            public static Dictionary<ulong, Dictionary<ulong, List<NetworkObject>>> DistributedObjects = new Dictionary<ulong, Dictionary<ulong, List<NetworkObject>>>();

            public bool AllowOwnershipRequest = true;
            public bool OnlyAllowTargetClientId = false;
            public ulong ClientToAllowOwnership;

            public NetworkObject.OwnershipRequestResponseStatus OwnershipRequestResponseStatus { get; private set; }

            public NetworkObject.OwnershipPermissionsFailureStatus OwnershipPermissionsFailureStatus { get; private set; }

            public NetworkObject.OwnershipRequestResponseStatus ExpectOwnershipRequestResponseStatus { get; set; }

            public override void OnNetworkSpawn()
            {
                NetworkObject.OnOwnershipRequested = OnOwnershipRequested;
                NetworkObject.OnOwnershipRequestResponse = OnOwnershipRequestResponse;
                NetworkObject.OnOwnershipPermissionsFailure = OnOwnershipPermissionsFailure;

                base.OnNetworkSpawn();
            }

            private bool OnOwnershipRequested(ulong clientId)
            {
                // If we are not allowing any client to request (without locking), then deny all requests
                if (!AllowOwnershipRequest)
                {
                    return false;
                }

                // If we are only allowing a specific client and the requesting client is not the target,
                // then deny the request
                if (OnlyAllowTargetClientId && clientId != ClientToAllowOwnership)
                {
                    return false;
                }

                // Otherwise, approve the request
                return true;
            }

            private void OnOwnershipRequestResponse(NetworkObject.OwnershipRequestResponseStatus ownershipRequestResponseStatus)
            {
                OwnershipRequestResponseStatus = ownershipRequestResponseStatus;
            }

            private void OnOwnershipPermissionsFailure(NetworkObject.OwnershipPermissionsFailureStatus ownershipPermissionsFailureStatus)
            {
                OwnershipPermissionsFailureStatus = ownershipPermissionsFailureStatus;
            }

            public override void OnNetworkDespawn()
            {
                NetworkObject.OnOwnershipRequested = null;
                NetworkObject.OnOwnershipRequestResponse = null;
                base.OnNetworkSpawn();
            }

            protected override void OnOwnershipChanged(ulong previous, ulong current)
            {
                if (current == NetworkManager.LocalClientId)
                {
                    CurrentOwnedInstance = NetworkObject;
                }
                base.OnOwnershipChanged(previous, current);
            }
        }
    }
}
