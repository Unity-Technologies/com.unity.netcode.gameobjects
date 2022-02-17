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
        public ulong NetworkObjectId;
        public ushort NetworkBehaviourIndex;

        public HashSet<int> DeliveryMappedNetworkVariableIndex;
        public ulong ClientId;
        public NetworkBehaviour NetworkBehaviour;

        private FastBufferReader m_ReceivedNetworkVariableData;

        public void Serialize(FastBufferWriter writer)
        {
            if (!writer.TryBeginWrite(FastBufferWriter.GetWriteSize(NetworkObjectId) + FastBufferWriter.GetWriteSize(NetworkBehaviourIndex)))
            {
                throw new OverflowException($"Not enough space in the buffer to write {nameof(NetworkVariableDeltaMessage)}");
            }

            writer.WriteValue(NetworkObjectId);
            writer.WriteValue(NetworkBehaviourIndex);

            for (int k = 0; k < NetworkBehaviour.NetworkVariableFields.Count; k++)
            {
                if (!DeliveryMappedNetworkVariableIndex.Contains(k))
                {
                    // This var does not belong to the currently iterating delivery group.
                    if (NetworkBehaviour.NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        writer.WriteValueSafe((ushort)0);
                    }
                    else
                    {
                        writer.WriteValueSafe(false);
                    }

                    continue;
                }

                //   if I'm dirty AND a client, write (server always has all permissions)
                //   if I'm dirty AND the server AND the client can read me, send.
                bool shouldWrite = NetworkBehaviour.NetworkVariableFields[k].ShouldWrite(ClientId, NetworkBehaviour.NetworkManager.IsServer);

                if (NetworkBehaviour.NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                {
                    if (!shouldWrite)
                    {
                        writer.WriteValueSafe((ushort)0);
                    }
                }
                else
                {
                    writer.WriteValueSafe(shouldWrite);
                }

                if (shouldWrite)
                {
                    if (NetworkBehaviour.NetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                    {
                        var tmpWriter = new FastBufferWriter(MessagingSystem.NON_FRAGMENTED_MESSAGE_MAX_SIZE, Allocator.Temp, short.MaxValue);
                        NetworkBehaviour.NetworkVariableFields[k].WriteDelta(tmpWriter);

                        if (!writer.TryBeginWrite(FastBufferWriter.GetWriteSize<ushort>() + tmpWriter.Length))
                        {
                            throw new OverflowException($"Not enough space in the buffer to write {nameof(NetworkVariableDeltaMessage)}");
                        }

                        writer.WriteValue((ushort)tmpWriter.Length);
                        tmpWriter.CopyTo(writer);
                    }
                    else
                    {
                        NetworkBehaviour.NetworkVariableFields[k].WriteDelta(writer);
                    }

                    if (!NetworkBehaviour.NetworkVariableIndexesToResetSet.Contains(k))
                    {
                        NetworkBehaviour.NetworkVariableIndexesToResetSet.Add(k);
                        NetworkBehaviour.NetworkVariableIndexesToReset.Add(k);
                    }

                    NetworkBehaviour.NetworkManager.NetworkMetrics.TrackNetworkVariableDeltaSent(
                        ClientId,
                        NetworkBehaviour.NetworkObject,
                        NetworkBehaviour.NetworkVariableFields[k].Name,
                        NetworkBehaviour.__getTypeName(),
                        writer.Length);
                }
            }
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            if (!reader.TryBeginRead(FastBufferWriter.GetWriteSize(NetworkObjectId) + FastBufferWriter.GetWriteSize(NetworkBehaviourIndex)))
            {
                throw new OverflowException($"Not enough data in the buffer to read {nameof(NetworkVariableDeltaMessage)}");
            }

            reader.ReadValue(out NetworkObjectId);
            reader.ReadValue(out NetworkBehaviourIndex);

            m_ReceivedNetworkVariableData = reader;

            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;

            if (networkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out NetworkObject networkObject))
            {
                NetworkBehaviour behaviour = networkObject.GetNetworkBehaviourAtOrderIndex(NetworkBehaviourIndex);

                if (behaviour == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Network variable delta message received for a non-existent behaviour. {nameof(NetworkObjectId)}: {NetworkObjectId}, {nameof(NetworkBehaviourIndex)}: {NetworkBehaviourIndex}");
                    }
                }
                else
                {
                    for (int i = 0; i < behaviour.NetworkVariableFields.Count; i++)
                    {
                        ushort varSize = 0;

                        if (networkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                        {
                            m_ReceivedNetworkVariableData.ReadValueSafe(out varSize);

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

                        if (networkManager.IsServer)
                        {
                            // we are choosing not to fire an exception here, because otherwise a malicious client could use this to crash the server
                            if (networkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                                {
                                    NetworkLog.LogWarning($"Client wrote to {typeof(NetworkVariable<>).Name} without permission. => {nameof(NetworkObjectId)}: {NetworkObjectId} - {nameof(NetworkObject.GetNetworkBehaviourOrderIndex)}(): {networkObject.GetNetworkBehaviourOrderIndex(behaviour)} - VariableIndex: {i}");
                                    NetworkLog.LogError($"[{behaviour.NetworkVariableFields[i].GetType().Name}]");
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
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                            {
                                NetworkLog.LogError($"Client wrote to {typeof(NetworkVariable<>).Name} without permission. No more variables can be read. This is critical. => {nameof(NetworkObjectId)}: {NetworkObjectId} - {nameof(NetworkObject.GetNetworkBehaviourOrderIndex)}(): {networkObject.GetNetworkBehaviourOrderIndex(behaviour)} - VariableIndex: {i}");
                                NetworkLog.LogError($"[{behaviour.NetworkVariableFields[i].GetType().Name}]");
                            }

                            return;
                        }
                        int readStartPos = m_ReceivedNetworkVariableData.Position;

                        behaviour.NetworkVariableFields[i].ReadDelta(m_ReceivedNetworkVariableData, networkManager.IsServer);

                        networkManager.NetworkMetrics.TrackNetworkVariableDeltaReceived(
                            context.SenderId,
                            networkObject,
                            behaviour.NetworkVariableFields[i].Name,
                            behaviour.__getTypeName(),
                            context.MessageSize);


                        if (networkManager.NetworkConfig.EnsureNetworkVariableLengthSafety)
                        {
                            if (m_ReceivedNetworkVariableData.Position > (readStartPos + varSize))
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                                {
                                    NetworkLog.LogWarning($"Var delta read too far. {m_ReceivedNetworkVariableData.Position - (readStartPos + varSize)} bytes. => {nameof(NetworkObjectId)}: {NetworkObjectId} - {nameof(NetworkObject.GetNetworkBehaviourOrderIndex)}(): {networkObject.GetNetworkBehaviourOrderIndex(behaviour)} - VariableIndex: {i}");
                                }

                                m_ReceivedNetworkVariableData.Seek(readStartPos + varSize);
                            }
                            else if (m_ReceivedNetworkVariableData.Position < (readStartPos + varSize))
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                                {
                                    NetworkLog.LogWarning($"Var delta read too little. {(readStartPos + varSize) - m_ReceivedNetworkVariableData.Position} bytes. => {nameof(NetworkObjectId)}: {NetworkObjectId} - {nameof(NetworkObject.GetNetworkBehaviourOrderIndex)}(): {networkObject.GetNetworkBehaviourOrderIndex(behaviour)} - VariableIndex: {i}");
                                }

                                m_ReceivedNetworkVariableData.Seek(readStartPos + varSize);
                            }
                        }
                    }
                }
            }
            else
            {
                networkManager.SpawnManager.TriggerOnSpawn(NetworkObjectId, m_ReceivedNetworkVariableData, ref context);
            }
        }
    }
}
