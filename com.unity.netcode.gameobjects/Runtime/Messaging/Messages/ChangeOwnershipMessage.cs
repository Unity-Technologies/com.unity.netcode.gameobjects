
using System.Runtime.CompilerServices;

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
            OwnershipChanging = 1,
            OwnershipFlagsUpdate = 2,
            RequestOwnership = 4,
            RequestApproved = 8,
            RequestDenied = 10,
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

                if (ChangeMessageType is ChangeType.OwnershipFlagsUpdate or ChangeType.OwnershipChanging or ChangeType.RequestApproved)
                {
                    writer.WriteValueSafe(OwnershipFlags);
                }

                // When requesting, RequestClientId is the requestor
                // When approving, RequestClientId is the owner that approved
                // When denied, RequestClientId is the requestor
                if (ChangeMessageType is ChangeType.RequestOwnership or ChangeType.RequestApproved or ChangeType.RequestDenied)
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
                    var clientId = (ulong)0;
                    for (int i = 0; i < ClientIdCount; i++)
                    {
                        ByteUnpacker.ReadValueBitPacked(reader, out clientId);
                        ClientIds[i] = clientId;
                    }
                }

                reader.ReadValueSafe(out ChangeMessageType);
                if (ChangeMessageType is ChangeType.OwnershipFlagsUpdate or ChangeType.OwnershipChanging or ChangeType.RequestApproved)
                {
                    reader.ReadValueSafe(out OwnershipFlags);
                }

                // When requesting, RequestClientId is the requestor
                // When approving, RequestClientId is the owner that approved
                // When denied, RequestClientId is the requestor
                if (ChangeMessageType is ChangeType.RequestOwnership or ChangeType.RequestApproved or ChangeType.RequestDenied)
                {
                    // We are receiving a request for ownership, or an approval or denial of our request.
                    reader.ReadValueSafe(out RequestClientId);

                    if (ChangeMessageType is ChangeType.RequestDenied)
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

            // If we are the DAHost then forward this message
            if (networkManager.DAHost)
            {
                var shouldProcessLocally = HandleDAHostMessageForwarding(ref networkManager, context.SenderId);
                if (!shouldProcessLocally)
                {
                    return;
                }
            }

            // If ownership is changing (either a straight change or a request approval), then run through the ownership changed sequence
            // Note: There is some extended ownership script at the bottom of HandleOwnershipChange
            // If not in distributed authority mode, ChangeMessageType will always be OwnershipChanging.
            if (ChangeMessageType is ChangeType.OwnershipChanging or ChangeType.RequestApproved)
            {
                HandleOwnershipChange(ref context);
            }
            else if (networkManager.DistributedAuthorityMode)
            {
                // Otherwise, we handle and extended ownership update
                HandleExtendedOwnershipUpdate(ref context);
            }
        }

        /// <summary>
        /// Handle the extended distributed authority ownership updates
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleExtendedOwnershipUpdate(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;

            // Handle the extended ownership message types
            var networkObject = networkManager.SpawnManager.SpawnedObjects[NetworkObjectId];

            if (ChangeMessageType is ChangeType.OwnershipFlagsUpdate)
            {
                // Just update the ownership flags
                networkObject.Ownership = (NetworkObject.OwnershipStatus)OwnershipFlags;
            }
            else if (ChangeMessageType is ChangeType.RequestOwnership)
            {
                // Requesting ownership, if allowed it will automatically send the ownership change message
                networkObject.OwnershipRequest(RequestClientId);
            }
            else if (ChangeMessageType is ChangeType.RequestDenied)
            {
                networkObject.OwnershipRequestResponse((NetworkObject.OwnershipRequestResponseStatus)OwnershipRequestResponseStatus);
            }
        }

        /// <summary>
        /// Handle the traditional change in ownership message type logic
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        private void HandleOwnershipChange(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            var networkObject = networkManager.SpawnManager.SpawnedObjects[NetworkObjectId];

            // Sanity check that we are not sending duplicated change ownership messages
            if (networkObject.OwnerClientId == OwnerClientId)
            {
                // Log error and then ignore the message
                NetworkLog.LogError($"[Receiver: Client-{networkManager.LocalClientId}][Sender: Client-{context.SenderId}][RID: {RequestClientId}] Detected unnecessary ownership changed message for {networkObject.name} (NID:{NetworkObjectId}).");
                return;
            }

            var originalOwner = networkObject.OwnerClientId;
            networkObject.OwnerClientId = OwnerClientId;

            // If in distributed authority mode
            if (networkManager.DistributedAuthorityMode)
            {
                networkObject.Ownership = (NetworkObject.OwnershipStatus)OwnershipFlags;

                networkObject.InvokeBehaviourOnLostOwnership();

                // Always update the network properties in distributed authority mode
                foreach (var child in networkObject.ChildNetworkBehaviours)
                {
                    child.UpdateNetworkProperties();
                }

                networkObject.InvokeBehaviourOnGainedOwnership();
            }
            else
            {
                // We are initial owner
                if (originalOwner == networkManager.LocalClientId)
                {
                    networkObject.InvokeBehaviourOnLostOwnership();
                }

                // For all other clients that are neither the former or current owner, update the behaviours' properties
                if (OwnerClientId != networkManager.LocalClientId && originalOwner != networkManager.LocalClientId)
                {
                    for (int i = 0; i < networkObject.ChildNetworkBehaviours.Count; i++)
                    {
                        networkObject.ChildNetworkBehaviours[i].UpdateNetworkProperties();
                    }
                }

                // We are new owner
                if (OwnerClientId == networkManager.LocalClientId)
                {
                    networkObject.InvokeBehaviourOnGainedOwnership();
                }

                if (originalOwner == networkManager.LocalClientId)
                {
                    // Fully synchronize NetworkVariables with either read or write ownership permissions.
                    networkObject.SynchronizeOwnerNetworkVariables(originalOwner, networkObject.PreviousOwnerId);
                }
            }

            // Always invoke ownership change notifications
            networkObject.InvokeOwnershipChanged(originalOwner, OwnerClientId);

            // If this change was requested, then notify that the request was approved (doing this last so all ownership
            // changes have already been applied if the callback is invoked)
            if (networkManager.DistributedAuthorityMode && networkManager.LocalClientId == OwnerClientId)
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
        /// <returns>true if this message should also be processed locally; false if the message should only be forwarded</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandleDAHostMessageForwarding(ref NetworkManager networkManager, ulong senderId)
        {
            var clientList = ClientIdCount > 0 ? ClientIds : networkManager.ConnectedClientsIds;

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
                    // We don't want the local DAHost's client to process this message, so exit early
                    return false;
                }
            }
            else if (ChangeMessageType == ChangeType.RequestOwnership)
            {
                // If the DAHost client is not authority, just forward the message to the authority
                if (OwnerClientId != networkManager.LocalClientId)
                {
                    networkManager.ConnectionManager.SendMessage(ref message, NetworkDelivery.Reliable, OwnerClientId);
                    // We don't want the local DAHost's client to process this message, so exit early
                    return false;
                }
                // Otherwise, fall through and process the request.
            }
            else
            {
                foreach (var clientId in clientList)
                {
                    if (clientId == networkManager.LocalClientId)
                    {
                        continue;
                    }

                    switch (ChangeMessageType)
                    {
                        // If ownership is changing and this is not an ownership request approval then ignore the SenderId
                        case ChangeType.OwnershipChanging when senderId == clientId:
                        // If it is just updating flags then ignore sending to the owner
                        case ChangeType.OwnershipFlagsUpdate when clientId == OwnerClientId:
                        // If it is a request approval, then ignore the RequestClientId
                        case ChangeType.RequestApproved when clientId == RequestClientId:
                            continue;
                    }

                    networkManager.ConnectionManager.SendMessage(ref message, NetworkDelivery.Reliable, clientId);
                }
            }

            // Return whether to process the message on the DAHost itself (only if object is spawned).
            return networkManager.SpawnManager.SpawnedObjects.ContainsKey(NetworkObjectId);
        }
    }
}
