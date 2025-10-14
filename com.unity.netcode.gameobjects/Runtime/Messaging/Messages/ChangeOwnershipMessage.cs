using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct ChangeOwnershipMessage : INetworkMessage, INetworkSerializeByMemcpy
    {
        public int Version => 0;

        private const string k_Name = "ChangeOwnershipMessage";

        public ulong NetworkObjectId;
        public ulong OwnerClientId;
        // SERVICE NOTES:
        // When forwarding the message to clients on the CMB Service side,
        // you can set the ClientIdCount to 0 and skip writing the ClientIds.
        // See the NetworkObjet.OwnershipRequest for more potential service side additions

        /// <summary>
        /// When requesting, RequestClientId is the requestor.
        /// When approving, RequestClientId is the owner that approved.
        /// When responding (only for denied), RequestClientId is the requestor
        /// </summary>
        internal ulong RequestClientId;
        internal int ClientIdCount;
        internal ulong[] ClientIds;
        internal bool DistributedAuthorityMode;
        internal ushort OwnershipFlags;
        internal byte OwnershipRequestResponseStatus;
        internal ChangeType ChangeMessageType;

        internal enum ChangeType : byte
        {
            OwnershipChanging = 0x01,
            OwnershipFlagsUpdate = 0x02,
            RequestOwnership = 0x04,
            RequestApproved = 0x08,
            RequestDenied = 0x10,
        }

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            BytePacker.WriteValueBitPacked(writer, NetworkObjectId);
            BytePacker.WriteValueBitPacked(writer, OwnerClientId);
            if (DistributedAuthorityMode)
            {
                BytePacker.WriteValueBitPacked(writer, ClientIdCount);
                if (ClientIdCount > 0)
                {
                    if (ClientIdCount != ClientIds.Length)
                    {
                        throw new System.Exception($"[{nameof(ChangeOwnershipMessage)}] ClientIdCount is {ClientIdCount} but the ClientIds length is {ClientIds.Length}!");
                    }
                    foreach (var clientId in ClientIds)
                    {
                        BytePacker.WriteValueBitPacked(writer, clientId);
                    }
                }

                writer.WriteValueSafe(ChangeMessageType);

                if (ChangeMessageType == ChangeType.OwnershipFlagsUpdate || ChangeMessageType == ChangeType.OwnershipChanging || ChangeMessageType == ChangeType.RequestApproved)
                {
                    writer.WriteValueSafe(OwnershipFlags);
                }

                // When requesting, RequestClientId is the requestor
                // When approving, RequestClientId is the owner that approved
                // When denied, RequestClientId is the requestor
                if (ChangeMessageType == ChangeType.RequestOwnership || ChangeMessageType == ChangeType.RequestApproved || ChangeMessageType == ChangeType.RequestDenied)
                {
                    writer.WriteValueSafe(RequestClientId);

                    if (ChangeMessageType is ChangeType.RequestDenied)
                    {
                        writer.WriteValueSafe(OwnershipRequestResponseStatus);
                    }
                }
            }
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }
            ByteUnpacker.ReadValueBitPacked(reader, out NetworkObjectId);
            ByteUnpacker.ReadValueBitPacked(reader, out OwnerClientId);

            if (networkManager.DistributedAuthorityMode)
            {
                ByteUnpacker.ReadValueBitPacked(reader, out ClientIdCount);
                if (ClientIdCount > 0)
                {
                    ClientIds = new ulong[ClientIdCount];
                    for (int i = 0; i < ClientIdCount; i++)
                    {
                        ByteUnpacker.ReadValueBitPacked(reader, out ulong clientId);
                        ClientIds[i] = clientId;
                    }
                }

                reader.ReadValueSafe(out ChangeMessageType);
                if (ChangeMessageType == ChangeType.OwnershipFlagsUpdate || ChangeMessageType == ChangeType.OwnershipChanging || ChangeMessageType == ChangeType.RequestApproved)
                {
                    reader.ReadValueSafe(out OwnershipFlags);
                }

                // When requesting, RequestClientId is the requestor
                // When approving, RequestClientId is the owner that approved
                // When denied, RequestClientId is the requestor
                if (ChangeMessageType == ChangeType.RequestOwnership || ChangeMessageType == ChangeType.RequestApproved || ChangeMessageType == ChangeType.RequestDenied)
                {
                    // We are receiving a request for ownership, or an approval or denial of our request.
                    reader.ReadValueSafe(out RequestClientId);

                    if (ChangeMessageType == ChangeType.RequestDenied)
                    {
                        reader.ReadValueSafe(out OwnershipRequestResponseStatus);
                    }
                }
            }
            else
            {
                // The only valid message type in Client/Server is ownership changing.
                ChangeMessageType = ChangeType.OwnershipChanging;
            }


            // If we are not a DAHost instance and the NetworkObject does not exist then defer it as it very likely is not spawned yet.
            // Otherwise if we are the DAHost and it does not exist then we want to forward this message because when the NetworkObject
            // is made visible again, the ownership flags and owner information will be synchronized with the DAHost by the current
            // authority of the NetworkObject in question.
            if (!networkManager.DAHost && !networkManager.SpawnManager.SpawnedObjects.ContainsKey(NetworkObjectId))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnSpawn, NetworkObjectId, reader, ref context, k_Name);
                return false;
            }
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            var hasObject = networkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out var networkObject);

            // If we are the DAHost then forward this message
            if (networkManager.DAHost)
            {
                var shouldProcessLocally = HandleDAHostMessageForwarding(ref networkManager, context.SenderId, hasObject, ref networkObject);
                if (!shouldProcessLocally)
                {
                    return;
                }
            }

            if (!hasObject)
            {
                if (networkManager.LogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogError("Ownership change received for an unknown network object. This should not happen.");
                }
                return;
            }

            // If ownership is changing (either a straight change or a request approval), then run through the ownership changed sequence
            // Note: There is some extended ownership script at the bottom of HandleOwnershipChange
            // If not in distributed authority mode, ChangeMessageType will always be OwnershipChanging.
            if (ChangeMessageType == ChangeType.OwnershipChanging || ChangeMessageType == ChangeType.RequestApproved || !networkManager.DistributedAuthorityMode)
            {
                HandleOwnershipChange(ref context, ref networkManager, ref networkObject);
            }
            else if (networkManager.DistributedAuthorityMode)
            {
                // Otherwise, we handle and extended ownership update
                HandleExtendedOwnershipUpdate(ref context, ref networkObject);
            }
        }

        /// <summary>
        /// Handle the extended distributed authority ownership updates
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleExtendedOwnershipUpdate(ref NetworkContext context, ref NetworkObject networkObject)
        {
            if (ChangeMessageType == ChangeType.OwnershipFlagsUpdate)
            {
                // Just update the ownership flags
                networkObject.Ownership = (NetworkObject.OwnershipStatus)OwnershipFlags;
            }
            else if (ChangeMessageType == ChangeType.RequestOwnership)
            {
                // Requesting ownership, if allowed it will automatically send the ownership change message
                networkObject.OwnershipRequest(RequestClientId);
            }
            else if (ChangeMessageType == ChangeType.RequestDenied)
            {
                networkObject.OwnershipRequestResponse((NetworkObject.OwnershipRequestResponseStatus)OwnershipRequestResponseStatus);
            }
        }

        /// <summary>
        /// Handle the traditional change in ownership message type logic
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleOwnershipChange(ref NetworkContext context, ref NetworkManager networkManager, ref NetworkObject networkObject)
        {
            var distributedAuthorityMode = networkManager.DistributedAuthorityMode;

            // Sanity check that we are not sending duplicated change ownership messages
            if (networkObject.OwnerClientId == OwnerClientId)
            {
                // Log error and then ignore the message
                NetworkLog.LogError($"[Receiver: Client-{networkManager.LocalClientId}][Sender: Client-{context.SenderId}][RID: {RequestClientId}] Detected unnecessary ownership changed message for {networkObject.name} (NID:{NetworkObjectId}).");
                return;
            }

            var originalOwner = networkObject.OwnerClientId;
            networkObject.OwnerClientId = OwnerClientId;

            if (distributedAuthorityMode)
            {
                networkObject.Ownership = (NetworkObject.OwnershipStatus)OwnershipFlags;
            }

            // Notify lost ownership, update the ownership, then notify gained ownership for the network behaviours
            networkObject.InvokeBehaviourOnOwnershipChanged(originalOwner, OwnerClientId);

            if (!distributedAuthorityMode && originalOwner == networkManager.LocalClientId)
            {
                // Fully synchronize NetworkVariables with either read or write ownership permissions.
                networkObject.SynchronizeOwnerNetworkVariables(originalOwner, networkObject.PreviousOwnerId);
            }

            // Always invoke ownership change notifications
            networkObject.InvokeOwnershipChanged(originalOwner, OwnerClientId);

            // If this change was requested, then notify that the request was approved (doing this last so all ownership
            // changes have already been applied if the callback is invoked)
            if (distributedAuthorityMode && networkManager.LocalClientId == OwnerClientId)
            {
                if (ChangeMessageType is ChangeType.RequestApproved)
                {
                    networkObject.OwnershipRequestResponse(NetworkObject.OwnershipRequestResponseStatus.Approved);
                }

                // If the NetworkObject changed ownership and the Requested flag was set (i.e. it was an ownership request),
                // then the new owner granted ownership removes the Requested flag and sends out an ownership status update.
                if (networkObject.HasExtendedOwnershipStatus(NetworkObject.OwnershipStatusExtended.Requested))
                {
                    networkObject.RemoveOwnershipExtended(NetworkObject.OwnershipStatusExtended.Requested);
                    networkObject.SendOwnershipStatusUpdate();
                }
            }

            networkManager.NetworkMetrics.TrackOwnershipChangeReceived(context.SenderId, networkObject, context.MessageSize);
        }

        /// <summary>
        /// [DAHost Only]
        /// Forward this message to all other clients who need to receive it.
        /// </summary>
        /// <param name="networkManager">The current NetworkManager from the NetworkContext</param>
        /// <param name="senderId">The sender of the current message from the NetworkContext</param>
        /// <param name="hasObject">Whether the local client has this object spawned</param>
        /// <param name="networkObject">The networkObject we are changing ownership on. Will be null if hasObject is false.</param>
        /// <returns>true if this message should also be processed locally; false if the message should only be forwarded</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandleDAHostMessageForwarding(ref NetworkManager networkManager, ulong senderId, bool hasObject, ref NetworkObject networkObject)
        {
            var message = new ChangeOwnershipMessage()
            {
                NetworkObjectId = NetworkObjectId,
                OwnerClientId = OwnerClientId,
                DistributedAuthorityMode = true,
                OwnershipFlags = OwnershipFlags,
                RequestClientId = RequestClientId,
                ClientIdCount = 0,
                ChangeMessageType = ChangeMessageType,
            };

            if (ChangeMessageType == ChangeType.RequestDenied)
            {
                // If the local DAHost's client is not the target, then forward to the target
                if (RequestClientId != networkManager.LocalClientId)
                {
                    message.OwnershipRequestResponseStatus = OwnershipRequestResponseStatus;
                    networkManager.ConnectionManager.SendMessage(ref message, NetworkDelivery.Reliable, RequestClientId);

                    // We don't want the local DAHost's client to process this message
                    return false;
                }
            }
            else if (ChangeMessageType == ChangeType.RequestOwnership)
            {
                // If the DAHost client is not authority, just forward the message to the authority
                if (OwnerClientId != networkManager.LocalClientId)
                {
                    networkManager.ConnectionManager.SendMessage(ref message, NetworkDelivery.Reliable, OwnerClientId);

                    // We don't want the local DAHost's client to process this message
                    return false;
                }
            }
            else
            {
                var clientList = ClientIds;
                var errorOnSender = true;

                // OwnershipFlagsUpdate doesn't populate the ClientIds list.
                if (ChangeMessageType == ChangeType.OwnershipFlagsUpdate)
                {
                    // if the DAHost can see this object, forward the message to all observers.
                    // if the DAHost can't see the object, forward the message to everyone.
                    clientList = hasObject ? networkObject.Observers.ToArray() : networkManager.ConnectedClientsIds.ToArray();

                    // Both clientList arrays will have the local client so we can not throw an error.
                    errorOnSender = false;
                }

                foreach (var clientId in clientList)
                {
                    // Don't forward to self or originating client
                    if (clientId == networkManager.LocalClientId)
                    {
                        continue;
                    }

                    if (clientId == senderId)
                    {
                        if (errorOnSender)
                        {
                            Debug.LogError($"client-{senderId} sent a ChangeOwnershipMessage with themself inside the ClientIds list.");
                        }

                        continue;
                    }

                    networkManager.ConnectionManager.SendMessage(ref message, NetworkDelivery.Reliable, clientId);
                }
            }

            // Return whether to process the message on the DAHost itself (only if object is spawned).
            return hasObject;
        }
    }
}
