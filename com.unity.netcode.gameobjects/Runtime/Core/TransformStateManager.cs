
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
// For JobHandle & IJobParallelForTransform
using Unity.Jobs;
using UnityEngine;
// For TransformAccess & TransformAccessArray
using UnityEngine.Jobs;

namespace Unity.Netcode
{
    /// <summary>
    /// 
    /// </summary>
    [BurstCompile]
    internal struct CheckTransformStateDeltasJob : IJobParallelForTransform
    {
        public bool NextTick;
        public int Precision;

        // The current state
        public NativeArray<TransformState> Current;

        // This runs in parallel for each transform
        public void Execute(int index, TransformAccess transform)
        {
            if (!transform.isValid)
            {
                return;
            }
            var current = Current[index];

            current.ProcessCurrentState(index, transform, Precision, NextTick);

            Current[index] = current;
        }

        public int HasDeltas()
        {
            if (!Current.IsCreated)
            {
                return 0;
            }
            //return DirtyCount;
            var deltaCount = 0;
            for (int i = 0; i < Current.Length; i++)
            {
                //if (Current[i].Delta.HasDelta())
                if (Current[i].GridStateDelta.HasDelta())
                {
                    deltaCount++;
                }
            }
            return deltaCount;
        }
    }


    public class TransformStateManager : IDisposable
    {
        internal bool DebugMode;

        internal FastBufferWriter FastBufferWriter = new FastBufferWriter(1024 * 256, Allocator.Persistent);

        private TransformAccessArray m_TransformAccessArray;
        private JobHandle m_JobHandle;

        private Dictionary<ulong, Dictionary<ushort, TransformStateSync>> m_TransformStates = new Dictionary<ulong, Dictionary<ushort, TransformStateSync>>();
        private NativeArray<TransformState> m_NativeStates;

        private List<TransformStateSync> m_SpawnedInstances = new List<TransformStateSync>();

        private NetworkManager m_NetworkManager;
        private int m_Precision = 100;

        private bool m_JobRunning;
        private int m_LastTickUpdate;

        private NetworkTime m_LocalTime;

        private CheckTransformStateDeltasJob m_CurrentJob;

        private void InitializeNativeStates(bool allocate = true)
        {
            if (allocate)
            {
                m_TransformAccessArray = new TransformAccessArray(m_SpawnedInstances.Count, 1);
                m_NativeStates = new NativeArray<TransformState>(m_SpawnedInstances.Count, Allocator.Persistent);
            }
            else if (m_TransformAccessArray.length != m_NativeStates.Length)
            {
                // TODO: Determine if we should log a warning here.
                // Edge case when destroying a bunch of things.
                return;
            }
            // Assure our transform access array is aligned with our native states array.
            for (int i = 0; i < m_NativeStates.Length; i++)
            {
                var instance = m_SpawnedInstances[i];
                if (allocate)
                {
                    m_TransformAccessArray.Add(instance.transform);
                }
                else
                {
                    m_TransformAccessArray.SetTransformHandle(i, instance.transform.transformHandle);
                }

                var state = m_NativeStates[i];
                if (allocate)
                {
                    state.GridStateDelta.Initialize();
                }
                state.UpdateIds(instance);
                m_NativeStates[i] = state;
            }
        }

        internal void TrackTransformStateChanges(TransformStateSync transformStateSync, bool isSpawned)
        {
            if (isSpawned)
            {
                OnInstanceSpawned(transformStateSync);
            }
            else
            {
                OnInstanceDespawning(transformStateSync);
            }
        }

        private void OnInstanceSpawned(TransformStateSync instance)
        {
            if (m_SpawnedInstances == null)
            {
                m_SpawnedInstances = new List<TransformStateSync>();
            }
            if (m_TransformStates == null)
            {
                m_TransformStates = new Dictionary<ulong, Dictionary<ushort, TransformStateSync>>();
            }

            // Authority
            if (instance.IsMotionAuthority)
            {
                // Track transform changes
                m_SpawnedInstances.Add(instance);
            }

            // Create a lookup table for for everything spawned.
            if (!m_TransformStates.ContainsKey(instance.NetworkObjectId))
            {
                m_TransformStates.Add(instance.NetworkObjectId, new Dictionary<ushort, TransformStateSync>());
            }
            if (!m_TransformStates[instance.NetworkObjectId].ContainsKey(instance.NetworkBehaviourId))
            {
                m_TransformStates[instance.NetworkObjectId].Add(instance.NetworkBehaviourId, instance);
            }
        }

        private void OnInstanceDespawning(TransformStateSync instance)
        {
            var index = m_SpawnedInstances.IndexOf(instance);
            if (index < 0)
            {
                return;
            }
            if (m_TransformAccessArray.isCreated && m_TransformAccessArray.length > index)
            {
                m_TransformAccessArray.RemoveAtSwapBack(index);
            }

            if (m_TransformStates.ContainsKey(instance.NetworkObjectId))
            {
                m_TransformStates[instance.NetworkObjectId].Remove(instance.NetworkBehaviourId);
            }
            if (m_SpawnedInstances.Count > index)
            {
                m_SpawnedInstances.RemoveAt(index);
            }
        }

        internal void Initialize(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            m_LastTickUpdate = networkManager.LocalTime.Tick;
        }

        internal void OnEarlyUpdate()
        {
            // Only update when there is something to update
            if (m_SpawnedInstances.Count > 0)
            {
                m_LocalTime = m_NetworkManager.LocalTime;
                if (m_LocalTime.Tick != m_LastTickUpdate)
                {
                    if (m_NativeStates == null || (m_NativeStates.Length != m_SpawnedInstances.Count))
                    {
                        DisposeNativeStates();
                        InitializeNativeStates();
                    }
                    else
                    {
                        InitializeNativeStates(false);
                    }

                    m_CurrentJob = new CheckTransformStateDeltasJob
                    {
                        Current = m_NativeStates,
                        Precision = m_Precision,
                        NextTick = true
                    };

                    m_JobHandle = m_CurrentJob.Schedule(m_TransformAccessArray);
                    m_JobRunning = true;
                }
            }
        }

        private int m_MessageTicketNumber = 0;

        internal void OnPreLateUpdate()
        {
            if (m_JobRunning && m_LocalTime.Tick != m_LastTickUpdate)
            {
                var lastTick = m_LastTickUpdate;
                m_LastTickUpdate = m_LocalTime.Tick;
                // Ensure the job is completed before the next frame
                m_JobHandle.Complete();
                m_JobRunning = false;
#if DEBUGDELTACOMPRESSION
                DebugJobResults();
#endif
                AvBytesPerUpdate = 0;
                AvHeaderSize = 0;
                AvPayLoadSize = 0;
                if (m_CurrentJob.HasDeltas() > 0)
                {
                    FastBufferWriter.Seek(0);
                    var startOfBuffer = FastBufferWriter.Position;
                    var count = (ushort)0;
                    var tick = m_NetworkManager.LocalTime.Tick;
                    BytePacker.WriteValueBitPacked(FastBufferWriter, m_NetworkManager.LocalClientId);
                    //BytePacker.WriteValueBitPacked(FastBufferWriter, m_MessageTicketNumber);
                    BytePacker.WriteValueBitPacked(FastBufferWriter, tick);

                    var offset = FastBufferWriter.Position;
                    var lastPosition = FastBufferWriter.Position;
                    var internalCount = 0;
#if DEBUG_TRANSFORMSTATE
                    NetworkLog.LogInfo($"[{nameof(TransformStateManager)}][Send] ======================(BEGIN - {m_MessageTicketNumber} Header: {FastBufferWriter.Position - startOfBuffer})======================");
#endif
                    foreach (var entry in m_CurrentJob.Current)
                    {
                        if (entry.GridStateDelta.HasDelta())
                        {
                            var start = FastBufferWriter.Position;
                            var writeSize = entry.GridStateDelta.DebugWriteState(FastBufferWriter);
                            var totalSize = FastBufferWriter.Position - start;

#if DEBUG_TRANSFORMSTATE
                            var header = $"[{nameof(TransformStateManager)}][Send][NetworkObjectId: {entry.GridStateDelta.TransformIdentifier}][Index: {entry.GridStateDelta.Index}][Total Size: {totalSize}][Header: {writeSize.Item2}][PayloadSize: {writeSize.Item3}]";
                            if ((writeSize.Item1 & 0x01) == 0x01)
                            {
                                header += $"[S: {entry.GridStateDelta.Scale.ToVector3()}]";
                            }

                            if ((writeSize.Item1 & 0x02) == 0x02)
                            {
                                header += $"[P: {entry.GridStateDelta.Position.ToVector3()}]";
                            }

                            if ((writeSize.Item1 & 0x04) == 0x04)
                            {
                                header += $"[R: {entry.GridStateDelta.Rotation.Rotation}]";
                            }
                            NetworkLog.LogInfo(header);
#endif
                            count++;
                            var readSize = FastBufferWriter.Position - lastPosition;
                            AvBytesPerUpdate = AvBytesPerUpdate == 0 ? readSize : (int)(0.5f * (AvBytesPerUpdate + readSize));
                            AvHeaderSize = AvHeaderSize == 0 ? entry.GridStateDelta.Header_Size : (int)(0.5f * (AvHeaderSize + entry.GridStateDelta.Header_Size));
                            AvPayLoadSize = AvPayLoadSize == 0 ? entry.GridStateDelta.Payload_Size : (int)(0.5f * (AvPayLoadSize + entry.GridStateDelta.Payload_Size));
                            lastPosition = FastBufferWriter.Position;
                            internalCount++;
                        }
                    }
                    if (internalCount == 0)
                    {
#if DEBUG_TRANSFORMSTATE
                        NetworkLog.LogInfo($"[{nameof(TransformStateManager)}][Send] ======================(END - NO DATA TO SEND)======================");
#endif
                        FastBufferWriter.Seek(0);
                        return;
                    }
                    m_MessageTicketNumber++;
                    //var position = FastBufferWriter.Position;
                    //FastBufferWriter.Seek(offset);
                    //FastBufferWriter.WriteValueSafe(count);
                    //FastBufferWriter.Seek(position);
                    var delivery = MessageDelivery.GetDelivery(NetworkMessageTypes.TransformStateUpdateMessage);
                    var transfromStateUpdateMessage = new TransformStateUpdateMessage()
                    {
                        State = FastBufferWriter.ToArray(),
                        Size  = FastBufferWriter.Position - startOfBuffer,
                        Count = count
                    };

                    if (m_NetworkManager.IsServer)
                    {
                        // TODO: Send an observer unique state buffer per client. Observer specific state buffers are not yet implemented so send to everyone.
                        unsafe
                        {
                            ulong* clients = stackalloc ulong[m_NetworkManager.ConnectedClientsIds.Count - 1];
                            var index = 0;
                            foreach (var clientId in m_NetworkManager.ConnectedClientsIds)
                            {
                                if (clientId == 0)
                                {
                                    continue;
                                }
                                clients[index] = clientId;
                                index++;
                            }
                            m_NetworkManager.ConnectionManager.SendMessage(ref transfromStateUpdateMessage, delivery, clients, index);
                            m_NetworkManager.NetworkMetrics.TrackTransportBytesReceived(transfromStateUpdateMessage.State.Length);
                        }
                    }
                    else
                    {
                        // TODO: Send an observer unique state buffer per client. Observer specific state buffers are not yet implemented so send to everyone.
                        m_NetworkManager.ConnectionManager.SendMessage(ref transfromStateUpdateMessage, delivery, NetworkManager.ServerClientId);
                        m_NetworkManager.NetworkMetrics.TrackTransportBytesReceived(transfromStateUpdateMessage.State.Length);
                    }

#if DEBUG_TRANSFORMSTATE
                    NetworkLog.LogInfo($"[{nameof(TransformStateManager)}][Send] ======================(END)======================");
#endif
                }
            }
        }
#if DEBUGDELTACOMPRESSION
        private void DebugJobResults()
        {
            var deltas = CurrentJob.HasDeltas();
            if (m_NextTick && deltas > 0)
            {
                // We would send the compressed deltas here.
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[TransformStateManager][Tick: {lastTick}] DirtyTransforms: {deltas}");
                            
                for(int i = 0; i < CurrentJob.Current.Length; i++)
                {
                    var entry = CurrentJob.Current[i];
                    var entryTransform = m_Transforms[i];
                    if (entry.Delta.HasDelta())
                    {
                        sb.Append($"[{entryTransform.name}][Deltas] Total size: {entry.Delta.TotalCompressedSize} of a possible {sizeof(int) * 10}:\n");
                        //if (entry.Delta.Scale.HasDelta())
                        //{
                        //    sb.Append($"Scale: Compressed down to ({entry.Delta.Scale.CompressedSize}) bytes of possible {sizeof(int) * 3} | ");
                        //}
                        if (entry.Delta.Position.HasDelta())
                        {
                            sb.Append($"Position: Compressed down to ({entry.Delta.Position.CompressedSize}) bytes of possible {sizeof(int) * 3} | ");
                            sb.Append($"Position: Decompressed ({entry.Delta.DecompressedPosition}) vs Original ({entry.Delta.OriginalPosition}).");
                        }
                        //if (entry.Delta.Rotation.HasDelta())
                        //{
                        //    sb.Append($"Rotation: Compressed down to ({entry.Delta.Rotation.CompressedSize}) bytes of possible {sizeof(int) * 4}.");
                        //}

                        sb.AppendLine();
                    }
                }
                Debug.Log(sb.ToString());
            }
        }
#endif
        public int AvBytesPerUpdate;
        public int AvHeaderSize;
        public int AvPayLoadSize;

        internal void UpdateTransformStates(ushort count, FastBufferReader reader)
        {
            var tick = 0;
            var transformState = new TransformGridState()
            {
                Precision = m_Precision,
                InvPrecision = 1.0f / m_Precision,
            };

            try
            {
                transformState.Initialize();
                var ticketNumber = 0;
                var clientSender = (ulong)0;
                ByteUnpacker.ReadValuePacked(reader, out clientSender);
                //ByteUnpacker.ReadValuePacked(reader, out ticketNumber);
                ByteUnpacker.ReadValuePacked(reader, out tick);
                //reader.ReadValueSafe(out count);
                var networkTime = new NetworkTime(m_NetworkManager.NetworkConfig.TickRate, tick);
                var lastPosition = reader.Position;
#if DEBUG_TRANSFORMSTATE
                NetworkLog.LogInfo($"[{nameof(TransformStateManager)}][{ticketNumber}][Receive][Count: {count}");
#endif
                for (var i = 0; i < count; i++)
                {
                    transformState.ReadState(reader);
#if DEBUG_TRANSFORMSTATE
                    var scale = (transformState.DirtyFlags & 0x01) == 0x01 ? $"[ScaleUpdated]" : string.Empty;
                    var position = (transformState.DirtyFlags & 0x02) == 0x02 ? $"[PositionUpdated]" : string.Empty;
                    var rotation = (transformState.DirtyFlags & 0x04) == 0x04 ? $"[RotationUpdated]" : string.Empty;
                    NetworkLog.LogInfo($"[Read][TransformIdentifier: {transformState.TransformIdentifier}]{scale}{position}{rotation}");
#endif

                    var identifierObjectMap = TransformStateSync.GetIdentifierObjectMap(clientSender, transformState.TransformIdentifier);
                    if (identifierObjectMap.NetworkObjectId == 0 && identifierObjectMap.NetworkBehaviourId == 0)
                    {
                        NetworkLog.LogWarningServer($"[{nameof(TransformStateManager)}][{ticketNumber}][Receive] Identifier ({transformState.TransformIdentifier}) has no object map! Skipping...");
                        continue;
                    }

                    if (m_TransformStates.ContainsKey(identifierObjectMap.NetworkObjectId))
                    {
                        if (m_TransformStates[identifierObjectMap.NetworkObjectId].ContainsKey(identifierObjectMap.NetworkBehaviourId))
                        {
                            var transformStateSync = m_TransformStates[identifierObjectMap.NetworkObjectId][identifierObjectMap.NetworkBehaviourId];
                            if (transformStateSync.TransformIdentifier != transformState.TransformIdentifier)
                            {
                                Debug.LogError($"!!!! Trying to update {transformStateSync.name} which has a local TID of {transformStateSync.TransformIdentifier} " +
                                    $"but incoming state update table thinks it is {transformState.TransformIdentifier}! (Ignoring entry)");
                                continue;
                            }

                            var readSize = reader.Position - lastPosition;
                            AvBytesPerUpdate = AvBytesPerUpdate == 0 ? readSize : (int)(0.5f * (AvBytesPerUpdate + readSize));
                            AvHeaderSize = AvHeaderSize == 0 ? transformState.Header_Size : (int)(0.5f * (AvHeaderSize + transformState.Header_Size));
                            AvPayLoadSize = AvPayLoadSize == 0 ? transformState.Payload_Size : (int)(0.5f * (AvPayLoadSize + transformState.Payload_Size));
                            transformState.Precision = m_Precision;
                            transformState.InvPrecision = 1.0f / m_Precision;
                            transformState.CurrentPosition = transformStateSync.transform.position;
                            transformState.CurrentScale = transformStateSync.transform.localScale;
                            transformState.Decompress();


                            m_TransformStates[identifierObjectMap.NetworkObjectId][identifierObjectMap.NetworkBehaviourId].UpdateState(networkTime.Time, transformState);
                        }
                        else if (DebugMode)
                        {
                            // TODO: This can trigger if a message is in flight with updates to objects just recently destroyed.
                            // This can happen in NGO where a message would typically be deferred and then dropped, but I think
                            // under this condition it is "ok" to silently ignore updates for things that no longer exist. This
                            // won't disrupt this deserialization process since the entry was read in the offset lines up for
                            // the next entry properly.
                            NetworkLog.LogWarningServer($"[{nameof(TransformStateManager)}][{nameof(TransformGridState)}] Read an entry for NetworkObjectId-{identifierObjectMap.NetworkObjectId} : " +
                                $"NetworkBehaviourId-{identifierObjectMap.NetworkBehaviourId}but it does not exist!");
                        }
                    }
                    else if (DebugMode)
                    {
                        // TODO: This can trigger if a message is in flight with updates to objects just recently destroyed.
                        // This can happen in NGO where a message would typically be deferred and then dropped, but I think
                        // under this condition it is "ok" to silently ignore updates for things that no longer exist. This
                        // won't disrupt this deserialization process since the entry was read in the offset lines up for
                        // the next entry properly.
                        NetworkLog.LogWarningServer($"[{nameof(TransformStateManager)}][{nameof(TransformGridState)}] Read an entry for NetworkObjectId-{identifierObjectMap.NetworkObjectId} but it does not exist!");
                    }
                    lastPosition = reader.Position;
                    transformState.Clear();
                }
            }
            catch (Exception ex)
            {
                transformState.Dispose();
                Debug.LogException(ex);
            }
        }

        private void DisposeNativeStates()
        {
            if (m_NativeStates == null)
            {
                return;
            }
            for (int i = 0; i < m_NativeStates.Length; i++)
            {
                var state = m_NativeStates[i];
                state.GridStateDelta.Dispose();
                m_NativeStates[i] = state;
            }
            if (m_NativeStates.IsCreated)
            {
                m_NativeStates.Dispose();
            }
            if (m_TransformAccessArray.isCreated)
            {
                m_TransformAccessArray.Dispose();
            }
        }

        public void Dispose()
        {
            DisposeNativeStates();
            FastBufferWriter.Dispose();
        }
    }
}
