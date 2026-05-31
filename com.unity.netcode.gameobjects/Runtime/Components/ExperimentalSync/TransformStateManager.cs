
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    /// The authority's transform state delta check job.
    /// </summary>
    [BurstCompile]
    internal struct CheckTransformStateDeltasJob : IJobParallelForTransform
    {
        public int Precision;

        public bool IsFullSynch;

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

            current.ProcessCurrentState(index, transform, Precision, IsFullSynch);

            Current[index] = current;
        }

        /// <summary>
        /// Returns the number of axis types that have deltas.
        /// </summary>
        /// <returns>number of axis types (position, rotation, and scale) that have deltas</returns>
        public int HasDeltas()
        {
            if (!Current.IsCreated)
            {
                return 0;
            }
            var deltaCount = 0;
            for (int i = 0; i < Current.Length; i++)
            {
                // We only need to check the delta state for...well...deltas.
                if (Current[i].GridStateDelta.HasDelta())
                {
                    deltaCount++;
                }
            }
            return deltaCount;
        }
    }

    /// <summary>
    /// Instantiated by <see cref="NetworkManager"/> and assigned to <see cref="NetworkManager.TransformStateManager"/>.
    /// This is the primary funnel for transform state updates as jobs.
    /// </summary>
    public class TransformStateManager : IDisposable
    {
        internal bool DebugMode;

        internal FastBufferWriter FastBufferWriter = new FastBufferWriter(1024 * 64, Allocator.Persistent);

        private TransformAccessArray m_TransformAccessArray;
        private JobHandle m_JobHandle;

        private Dictionary<ulong, Dictionary<ushort, TransformStateSync>> m_TransformStates = new Dictionary<ulong, Dictionary<ushort, TransformStateSync>>();
        private NativeArray<TransformState> m_NativeStates;

        private List<TransformStateSync> m_SpawnedInstances = new List<TransformStateSync>();

        private NetworkManager m_NetworkManager;
        /// <summary>
        /// This will be configurable via inspector view
        /// </summary>
        private int m_Precision = 1000;

        private bool m_JobRunning;
        private int m_LastTickUpdate;

        private NetworkTime m_LocalTime;

        private CheckTransformStateDeltasJob m_CurrentJob;

        private void InitializeNativeStates(bool allocate = true)
        {
            var prevNativeStateLength = 0;
            if (allocate)
            {
                m_TransformAccessArray = new TransformAccessArray(m_SpawnedInstances.Count, 1);

                var increasedArray = new NativeArray<TransformState>(m_SpawnedInstances.Count, Allocator.Persistent);
                if (m_NativeStates != null && m_NativeStates.IsCreated)
                {
                    prevNativeStateLength = m_NativeStates.Length;
                    NativeArray<TransformState>.Copy(m_NativeStates, increasedArray, prevNativeStateLength);
                    m_NativeStates.Dispose();
                }
                m_NativeStates = increasedArray;
            }
            else if (m_TransformAccessArray.length != m_NativeStates.Length)
            {
                // TODO: Determine if we should log a warning here.
                // Edge case when destroying a bunch of things.
                return;
            }
            // Assure our transform access array is aligned with our native states array.
            for (int i = 0; i < m_SpawnedInstances.Count; i++)
            {
                var instance = m_SpawnedInstances[i];
                if (allocate)
                {
                    m_TransformAccessArray.Add(instance.transform);
                    m_TransformAccessArray.SetTransformHandle(i, instance.transform.transformHandle);
                }
                else
                {
                    m_TransformAccessArray.SetTransformHandle(i, instance.transform.transformHandle);
                }
                var state = m_NativeStates[i];

                if (allocate && i >= prevNativeStateLength)
                {
                    state.Initialize();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanCheckForUpdates()
        {
            if (m_LocalTime.Tick != m_LastTickUpdate)
            {
                // Adjust based on bandwidth consumption
                if (m_LocalTime.Tick % m_TickModulus != 0)
                {
                    return false;
                }
            }
            ShouldSendFullSynch(m_LocalTime.Tick);
            return true;
        }

        private bool m_IsFullSynch;

        private void ShouldSendFullSynch(int tick)
        {
            m_IsFullSynch = (tick % m_NetworkManager.NetworkConfig.TickRate) == 0; // Bug in partial sync
        }

        internal void OnEarlyUpdate()
        {
            // Only update when there is something to update
            if (m_SpawnedInstances.Count > 0)
            {
                m_LocalTime = m_NetworkManager.LocalTime;
                if (CanCheckForUpdates())
                {
                    if (m_NativeStates == null || (m_NativeStates.Length != m_SpawnedInstances.Count))
                    {
                        DisposeNativeStates(true);
                        InitializeNativeStates();
                    }
                    else
                    {
                        InitializeNativeStates(false);
                    }
                    // Adjust based on bandwidth consumption
                    if ((m_LocalTime.Tick % m_TickModulus) != 0)
                    {
                        return;
                    }
                    m_CurrentJob = new CheckTransformStateDeltasJob
                    {
                        Current = m_NativeStates,
                        Precision = m_Precision,
                        IsFullSynch = m_IsFullSynch,
                    };

                    m_JobHandle = m_CurrentJob.Schedule(m_TransformAccessArray);
                    m_JobRunning = true;
                }
            }
        }

        private int m_MessageTicketNumber = 0;

        private int m_TickModulus = 1;
        internal void OnPreLateUpdate()
        {
            if (m_JobRunning)
            {
                // Ensure the job is completed before the next frame
                m_JobHandle.Complete();
                m_JobRunning = false;
                AvBytesPerUpdate = 0;
                AvHeaderSize = 0;
                AvPayLoadSize = 0;
                if (m_CurrentJob.HasDeltas() > 0)
                {
                    FastBufferWriter.Seek(0);
                    var startOfBuffer = FastBufferWriter.Position;
                    var count = (ushort)0;
                    var currentTick = m_NetworkManager.LocalTime.Tick;
                    m_LastTickUpdate = currentTick;

                    // The header for the internal processing. Adding values here for global changes in state
                    // has much less of an impact than adding additional data/bits to any axis type's serialized
                    // data.
                    var idInfo = (ushort)0;
                    // TODO: Determine if we need to provide more than 1 byte (255) potential clients.
                    idInfo = (byte)m_NetworkManager.LocalClientId;
                    // Pack the client id and full sync information together
                    idInfo = (byte)((idInfo << 1) | (m_IsFullSynch ? 1 : 0));
                    BytePacker.WriteValueBitPacked(FastBufferWriter, idInfo);
                    // For debugging purposes, add a ticket number to each message. Makes it easier
                    // to match on both the sender's and receiver's sides.
                    //BytePacker.WriteValueBitPacked(FastBufferWriter, m_MessageTicketNumber);

                    // Add the modulus to the tick;
                    currentTick = currentTick << 2 | (m_TickModulus & 3);
                    BytePacker.WriteValueBitPacked(FastBufferWriter, currentTick);

                    var offset = FastBufferWriter.Position;
                    var lastPosition = FastBufferWriter.Position;
                    var internalCount = 0;
                    var previousTransformIdentifier = (ushort)0;
#if DEBUG_TRANSFORMSTATE
                    NetworkLog.LogInfo($"[{nameof(TransformStateManager)}][Send] ======================(BEGIN - {m_MessageTicketNumber} Header: {FastBufferWriter.Position - startOfBuffer})======================");
#endif
                    // !!! Any additional data added to each transform's grid state will increase the bandwdith by:
                    // !!! (size added in bits or bytes) * (instances spawned) * (tick rate) --> Total bytes per second 
                    foreach (var entry in m_CurrentJob.Current)
                    {
                        if (entry.GridStateDelta.HasDelta())
                        {
                            var start = FastBufferWriter.Position;
                            var writeSize = entry.GridStateDelta.DebugWriteState(FastBufferWriter, previousTransformIdentifier);
                            previousTransformIdentifier = entry.GridStateDelta.TransformIdentifier;
                            var totalSize = FastBufferWriter.Position - start;
#if DEBUG_TRANSFORMSTATE
                            var header = $"[{nameof(TransformStateManager)}][Send][NetworkObjectId: {entry.GridStateDelta.TransformIdentifier}][Index: {entry.GridStateDelta.Index}][Total Size: {totalSize}][Header: {writeSize.Item2}][PayloadSize: {writeSize.Item3}]";
                            //if ((writeSize.Item1 & 0x01) == 0x01)
                            //{
                            //    header += $"[S: {entry.GridStateDelta.Scale.ToVector3()}]";
                            //}

                            if (entry.GridStateDelta.DirtyPosition)
                            {
                                var positionState = entry.GridStateDelta.Position;
                                entry.GridStateDelta.Position.Decompress();
                                var decompressed = entry.GridStateDelta.Position.ToVector3(1.0f / m_Precision);
                                if (m_IsFullSynch)
                                {
                                    header += $"[P-Decompressed: {decompressed}] vs [Current: {entry.GridStateCurrent.Position.Position}]";
                                }
                                else
                                {
                                    header += $"[P-Decompressed: {decompressed}] vs [P-OrignalDelta: {positionState.Delta}]";
                                }
                                    
                                header += $"[P: Comp-{positionState.CompressValuesAsString()}  Delta-{positionState.Delta}]";
                            }

                            if (entry.GridStateDelta.DirtyRotation)
                            {
                                header += $"[R: {entry.GridStateDelta.Rotation.Rotation}]";
                            }
                            NetworkLog.LogInfo(header);
#endif
                            count++;
                            var readSize = FastBufferWriter.Position - lastPosition;
                            AvBytesPerUpdate = AvBytesPerUpdate == 0 ? readSize : (int)(0.5f * (AvBytesPerUpdate + readSize));
                            AvHeaderSize = AvHeaderSize == 0 ? writeSize.Item2 : (int)(0.5f * (AvHeaderSize + writeSize.Item2));
                            AvPayLoadSize = AvPayLoadSize == 0 ? writeSize.Item3 : (int)(0.5f * (AvPayLoadSize + writeSize.Item3));
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
                    var totalUpdateSize = FastBufferWriter.Position - startOfBuffer;

                    // TODO:
                    // Make this maximum adjustable.
                    // Replace the tick modulus with a smoother transition where
                    // the "can update" is based on partial tick values while under
                    // a whole tick adjustment until it reaches the minimum update frequency
                    // which we might make adjustable as well.
                    // which is most likely going to
                    // TODO-Future:
                    // Make this adjustable based on RTT to client or service.
                    if (totalUpdateSize > 9000)
                    {
                        m_TickModulus = 2;
                    }
                    else
                    {
                        m_TickModulus = 1;
                    }
                    TickModulus = m_TickModulus;

                    AvTotalUpdateSize = AvTotalUpdateSize == 0 ? totalUpdateSize : (int)(0.5f * (AvTotalUpdateSize + totalUpdateSize));
                    m_MessageTicketNumber++;

                    var delivery = MessageDelivery.GetDelivery(NetworkMessageTypes.TransformStateUpdateMessage);
                    var transfromStateUpdateMessage = new TransformStateUpdateMessage()
                    {
                        State = FastBufferWriter.ToArray(),
                        Size = totalUpdateSize,
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

        public static int AvTotalUpdateSize;
        public static int AvBytesPerUpdate;
        public static int AvHeaderSize;
        public static int AvPayLoadSize;
        public static int TickModulus;

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
                var idInfo = (ushort)0;
                
                ByteUnpacker.ReadValuePacked(reader, out idInfo);
                transformState.IsFullSynch = (idInfo & 1) == 1;
                var clientSender = (ulong)(idInfo >> 1);
                // For debugging purposes, add a ticket number to each message. Makes it easier
                // to match on both the sender's and receiver's sides.
                //ByteUnpacker.ReadValuePacked(reader, out ticketNumber);
                ByteUnpacker.ReadValuePacked(reader, out tick);
                TickModulus = (tick & 3);
                tick = tick >> 2;

                var networkTime = new NetworkTime(m_NetworkManager.NetworkConfig.TickRate, tick);
                var lastPosition = reader.Position;
#if DEBUG_TRANSFORMSTATE
                NetworkLog.LogInfo($"[{nameof(TransformStateManager)}][{ticketNumber}][Receive][Count: {count}");
#endif
                var previousIdentifier = (ushort)0;
                for (var i = 0; i < count; i++)
                {
                    transformState.ReadStateWithPrevious(reader, previousIdentifier);
                    previousIdentifier = transformState.TransformIdentifier;
#if DEBUG_TRANSFORMSTATE
                    
                    var position = transformState.DirtyPosition ? $"[PositionUpdated]" : string.Empty;
                    var rotation = transformState.DirtyRotation ? $"[RotationUpdated]" : string.Empty;
                    var scale = transformState.DirtyScale ? $"[ScaleUpdated]" : string.Empty;
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

                            // For debugging purposes to get byte averages.
                            // Can be removed or the like later
                            var readSize = reader.Position - lastPosition;
                            AvBytesPerUpdate = AvBytesPerUpdate == 0 ? readSize : (int)(0.5f * (AvBytesPerUpdate + readSize));
                            AvHeaderSize = AvHeaderSize == 0 ? transformState.Header_Size : (int)(0.5f * (AvHeaderSize + transformState.Header_Size));
                            AvPayLoadSize = AvPayLoadSize == 0 ? transformState.Payload_Size : (int)(0.5f * (AvPayLoadSize + transformState.Payload_Size));
                            transformState.Precision = m_Precision;
                            transformState.InvPrecision = 1.0f / m_Precision;
                            transformState.LastPositionUpdate = transformStateSync.LastPositionUpdate;
                            transformState.CurrentScale = transformStateSync.transform.localScale;
                            transformState.Decompress();

                            m_TransformStates[identifierObjectMap.NetworkObjectId][identifierObjectMap.NetworkBehaviourId].UpdateState(networkTime.Time, m_TickModulus, transformState);
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

        private void DisposeNativeStates(bool ignoreNativeStates = false)
        {
            if (m_NativeStates == null)
            {
                return;
            }
            if (!ignoreNativeStates)
            {
                for (int i = 0; i < m_NativeStates.Length; i++)
                {
                    m_NativeStates[i].Dispose();
                }
                if (m_NativeStates.IsCreated)
                {
                    m_NativeStates.Dispose();
                }
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
            TransformStateSync.EndOfSessionReset();
        }
    }
}
