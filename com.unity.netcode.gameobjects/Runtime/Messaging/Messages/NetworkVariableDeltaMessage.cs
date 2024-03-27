using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Netcode
{
    /// <summary>
    /// This particular struct is a little weird because it doesn't actually contain the data
    /// it's serializing. Instead, it contains references to the data it needs to do the
    /// serialization. This is due to the generally amorphous nature of network variable
    /// deltas, since they're all driven by custom virtual method overloads.
    /// </summary>
    internal struct NetworkVariableDeltaMessage : INetworkMessage
    {
        public int Version => 0;

        public ulong NetworkObjectId;
        public ushort NetworkBehaviourIndex;

        public HashSet<int> DeliveryMappedNetworkVariableIndex;
        public ulong TargetClientId;
        public NetworkBehaviour NetworkBehaviour;

        private FastBufferReader m_ReceivedNetworkVariableData;

        // DANGO-TODO: Made some modifications here that overlap/won't play nice with EnsureNetworkVariableLenghtSafety.
        // Worth either merging or more cleanly separating these codepaths.
        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            if (!writer.TryBeginWrite(FastBufferWriter.GetWriteSize(NetworkObjectId) + FastBufferWriter.GetWriteSize(NetworkBehaviourIndex)))
            {
                throw new OverflowException($"Not enough space in the buffer to write {nameof(NetworkVariableDeltaMessage)}");
            }

            var obj = NetworkBehaviour.NetworkObject;
            var networkManager = obj.NetworkManagerOwner;

            BytePacker.WriteValueBitPacked(writer, NetworkObjectId);
            BytePacker.WriteValueBitPacked(writer, NetworkBehaviourIndex);
            if (networkManager.DistributedAuthorityMode)
            {
                writer.WriteValueSafe((ushort)NetworkBehaviour.NetworkVariableFields.Count);
            }

            for (int i = 0; i < NetworkBehaviour.NetworkVariableFields.Count; i++)
            {
                if (!DeliveryMappedNetworkVariableIndex.Contains(i))
                {
                    // This var does not belong to the currently iterating delivery group.
                    if (networkManager.DistributedAuthorityMode)
                    {
                        writer.WriteValueSafe<ushort>(0);
                    }
                    else if (networkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        BytePacker.WriteValueBitPacked(writer, (ushort)0);
                    }
                    else
                    {
                        writer.WriteValueSafe(false);
                    }

                    continue;
                }

                var startingSize = writer.Length;
                var networkVariable = NetworkBehaviour.NetworkVariableFields[i];
                var shouldWrite = networkVariable.IsDirty() &&
                    networkVariable.CanClientRead(TargetClientId) &&
                    (networkManager.IsServer || networkVariable.CanClientWrite(networkManager.LocalClientId));

                // Prevent the server from writing to the client that owns a given NetworkVariable
                // Allowing the write would send an old value to the client and cause jitter
                if (networkVariable.WritePerm == NetworkVariableWritePermission.Owner &&
                    networkVariable.OwnerClientId() == TargetClientId)
                {
                    shouldWrite = false;
                }

                // The object containing the behaviour we're about to process is about to be shown to this client
                // As a result, the client will get the fully serialized NetworkVariable and would be confused by
                // an extraneous delta
                if (networkManager.SpawnManager.ObjectsToShowToClient.ContainsKey(TargetClientId) &&
                    networkManager.SpawnManager.ObjectsToShowToClient[TargetClientId]
                    .Contains(obj))
                {
                    shouldWrite = false;
                }

                if (networkManager.DistributedAuthorityMode)
                {
                    if (!shouldWrite)
                    {
                        writer.WriteValueSafe<ushort>(0);
                    }
                }
                else if (networkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                {
                    if (!shouldWrite)
                    {
                        BytePacker.WriteValueBitPacked(writer, (ushort)0);
                    }
                }
                else
                {
                    writer.WriteValueSafe(shouldWrite);
                }

                if (shouldWrite)
                {
                    if (networkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        var tempWriter = new FastBufferWriter(networkManager.MessageManager.NonFragmentedMessageMaxSize, Allocator.Temp, networkManager.MessageManager.FragmentedMessageMaxSize);
                        NetworkBehaviour.NetworkVariableFields[i].WriteDelta(tempWriter);
                        BytePacker.WriteValueBitPacked(writer, tempWriter.Length);

                        if (!writer.TryBeginWrite(tempWriter.Length))
                        {
                            throw new OverflowException($"Not enough space in the buffer to write {nameof(NetworkVariableDeltaMessage)}");
                        }

                        tempWriter.CopyTo(writer);
                    }
                    else
                    {
                        // DANGO-TODO:
                        // Complex types with custom type serialization (either registered custom types or INetworkSerializable implementations) will be problematic
                        // Non-complex types always provide a full state update per delta
                        // DANGO-TODO: Add NetworkListEvent<T>.EventType awareness to the cloud-state server
                        if (networkManager.DistributedAuthorityMode)
                        {
                            var size_marker = writer.Position;
                            writer.WriteValueSafe<ushort>(0);
                            var start_marker = writer.Position;
                            networkVariable.WriteDelta(writer);
                            var end_marker = writer.Position;
                            writer.Seek(size_marker);
                            var size = end_marker - start_marker;
                            writer.WriteValueSafe((ushort)size);
                            writer.Seek(end_marker);
                        }
                        else
                        {
                            networkVariable.WriteDelta(writer);
                        }
                    }
                    networkManager.NetworkMetrics.TrackNetworkVariableDeltaSent(
                        TargetClientId,
                        obj,
                        networkVariable.Name,
                        NetworkBehaviour.__getTypeName(),
                        writer.Length - startingSize);
                }
            }
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            ByteUnpacker.ReadValueBitPacked(reader, out NetworkObjectId);
            ByteUnpacker.ReadValueBitPacked(reader, out NetworkBehaviourIndex);

            m_ReceivedNetworkVariableData = reader;

            return true;
        }

        // DANGO-TODO: Made some modifications here that overlap/won't play nice with EnsureNetworkVariableLenghtSafety.
        // Worth either merging or more cleanly separating these codepaths.
        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;

            if (networkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out NetworkObject networkObject))
            {
                var networkBehaviour = networkObject.GetNetworkBehaviourAtOrderIndex(NetworkBehaviourIndex);

                if (networkBehaviour == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Network variable delta message received for a non-existent behaviour. {nameof(NetworkObjectId)}: {NetworkObjectId}, {nameof(NetworkBehaviourIndex)}: {NetworkBehaviourIndex}");
                    }
                }
                else
                {
                    if (networkManager.DistributedAuthorityMode)
                    {
                        m_ReceivedNetworkVariableData.ReadValueSafe(out ushort variableCount);
                        if (variableCount != networkBehaviour.NetworkVariableFields.Count)
                        {
                            UnityEngine.Debug.LogError("Variable count mismatch");
                        }
                    }

                    for (int i = 0; i < networkBehaviour.NetworkVariableFields.Count; i++)
                    {
                        int varSize = 0;
                        if (networkManager.DistributedAuthorityMode)
                        {
                            m_ReceivedNetworkVariableData.ReadValueSafe(out ushort variableSize);
                            varSize = variableSize;

                            if (varSize == 0)
                            {
                                continue;
                            }
                        }
                        else if (networkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                        {
                            ByteUnpacker.ReadValueBitPacked(m_ReceivedNetworkVariableData, out varSize);

                            if (varSize == 0)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            m_ReceivedNetworkVariableData.ReadValueSafe(out bool deltaExists);
                            if (!deltaExists)
                            {
                                continue;
                            }
                        }

                        var networkVariable = networkBehaviour.NetworkVariableFields[i];

                        if (networkManager.IsServer && !networkVariable.CanClientWrite(context.SenderId))
                        {
                            // we are choosing not to fire an exception here, because otherwise a malicious client could use this to crash the server
                            if (networkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                                {
                                    NetworkLog.LogWarning($"Client wrote to {typeof(NetworkVariable<>).Name} without permission. => {nameof(NetworkObjectId)}: {NetworkObjectId} - {nameof(NetworkObject.GetNetworkBehaviourOrderIndex)}(): {networkObject.GetNetworkBehaviourOrderIndex(networkBehaviour)} - VariableIndex: {i}");
                                    NetworkLog.LogError($"[{networkVariable.GetType().Name}]");
                                }

                                m_ReceivedNetworkVariableData.Seek(m_ReceivedNetworkVariableData.Position + varSize);
                                continue;
                            }

                            //This client wrote somewhere they are not allowed. This is critical
                            //We can't just skip this field. Because we don't actually know how to dummy read
                            //That is, we don't know how many bytes to skip. Because the interface doesn't have a
                            //Read that gives us the value. Only a Read that applies the value straight away
                            //A dummy read COULD be added to the interface for this situation, but it's just being too nice.
                            //This is after all a developer fault. A critical error should be fine.
                            // - TwoTen
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                            {
                                NetworkLog.LogError($"Client wrote to {typeof(NetworkVariable<>).Name} without permission. No more variables can be read. This is critical. => {nameof(NetworkObjectId)}: {NetworkObjectId} - {nameof(NetworkObject.GetNetworkBehaviourOrderIndex)}(): {networkObject.GetNetworkBehaviourOrderIndex(networkBehaviour)} - VariableIndex: {i}");
                                NetworkLog.LogError($"[{networkVariable.GetType().Name}]");
                            }

                            return;
                        }
                        int readStartPos = m_ReceivedNetworkVariableData.Position;

                        // Read Delta so we also notify any subscribers to a change in the NetworkVariable
                        networkVariable.ReadDelta(m_ReceivedNetworkVariableData, networkManager.IsServer);

                        networkManager.NetworkMetrics.TrackNetworkVariableDeltaReceived(
                            context.SenderId,
                            networkObject,
                            networkVariable.Name,
                            networkBehaviour.__getTypeName(),
                            context.MessageSize);

                        if (networkManager.NetworkConfig.EnsureNetworkVariableLengthSafety || networkManager.DistributedAuthorityMode)
                        {
                            if (m_ReceivedNetworkVariableData.Position > (readStartPos + varSize))
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                                {
                                    NetworkLog.LogWarning($"Var delta read too far. {m_ReceivedNetworkVariableData.Position - (readStartPos + varSize)} bytes. => {nameof(NetworkObjectId)}: {NetworkObjectId} - {nameof(NetworkObject.GetNetworkBehaviourOrderIndex)}(): {networkObject.GetNetworkBehaviourOrderIndex(networkBehaviour)} - VariableIndex: {i}");
                                }

                                m_ReceivedNetworkVariableData.Seek(readStartPos + varSize);
                            }
                            else if (m_ReceivedNetworkVariableData.Position < (readStartPos + varSize))
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                                {
                                    NetworkLog.LogWarning($"Var delta read too little. {readStartPos + varSize - m_ReceivedNetworkVariableData.Position} bytes. => {nameof(NetworkObjectId)}: {NetworkObjectId} - {nameof(NetworkObject.GetNetworkBehaviourOrderIndex)}(): {networkObject.GetNetworkBehaviourOrderIndex(networkBehaviour)} - VariableIndex: {i}");
                                }

                                m_ReceivedNetworkVariableData.Seek(readStartPos + varSize);
                            }
                        }
                    }
                }
            }
            else
            {
                // DANGO-TODO: Fix me!
                // When a client-spawned NetworkObject is despawned by the owner client, the owner client will still get messages for deltas and cause this to
                // log a warning. The issue is primarily how NetworkVariables handle updating and will require some additional re-factoring.
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnSpawn, NetworkObjectId, m_ReceivedNetworkVariableData, ref context, GetType().Name);
            }
        }
    }
}
