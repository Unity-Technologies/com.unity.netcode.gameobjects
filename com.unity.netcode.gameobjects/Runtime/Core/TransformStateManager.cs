
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
    // UpdateTransformStateJob
    [BurstCompile]
    internal struct UpdateTransformStateJob : IJobParallelForTransform
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

        private UpdateTransformStateJob m_CurrentJob;

        internal FastBufferWriter FastBufferWriter = new FastBufferWriter(1024 * 256, Allocator.Persistent);

        private void InitializeNativeStates(bool allocate = true)
        {
            if (allocate)
            {
                m_TransformAccessArray = new TransformAccessArray(m_SpawnedInstances.Count, 1);
                m_NativeStates = new NativeArray<TransformState>(m_SpawnedInstances.Count, Allocator.Persistent);
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

        //private void RefreshNativeStates()
        //{
        //    for (int i = 0; i < m_TransformsArray.Length; i++)
        //    {
        //        var transform = m_TransformsArray[i];
        //        var state = m_NativeStates[i];
        //        var networkObjectId = transform.GetComponent<NetworkObject>().NetworkObjectId;
        //        var networkBehaviourId = transform.GetComponent<TransformStateSync>().NetworkBehaviourId;

        //        if (state.GridStateDelta.NetworkObjectId != networkObjectId)
        //        {
        //            Debug.LogWarning($"[Mismatch][State][NetworkObjectId] Index-{i} is {state.GridStateDelta.NetworkObjectId} when transform's is actually {networkObjectId}!");
        //        }
        //        if (state.GridStateDelta.NetworkBehaviourId != networkBehaviourId)
        //        {
        //            Debug.LogWarning($"[Mismatch][State][NetworkBehaviourId] Index-{i} is {state.GridStateDelta.NetworkBehaviourId} when transform's is actually {networkBehaviourId}!");
        //        }
        //        state.GridStateDelta.NetworkObjectId = state.GridStateCurrent.NetworkObjectId = networkObjectId;
        //        state.GridStateDelta.NetworkBehaviourId = state.GridStateCurrent.NetworkBehaviourId = networkBehaviourId;
        //        m_NativeStates[i] = state;
        //    }
        //}

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
            else
            {
                // Non-authority just creates a lookup table for receiving updates
                if (!m_TransformStates.ContainsKey(instance.NetworkObjectId))
                {
                    m_TransformStates.Add(instance.NetworkObjectId, new Dictionary<ushort, TransformStateSync>());
                }
                if (!m_TransformStates[instance.NetworkObjectId].ContainsKey(instance.NetworkBehaviourId))
                {
                    m_TransformStates[instance.NetworkObjectId].Add(instance.NetworkBehaviourId, instance);
                }
            }
        }

        private void OnInstanceDespawning(TransformStateSync instance)
        {
            var index = m_SpawnedInstances.IndexOf(instance);
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

        private Transform[] m_TransformsArray;



        internal void OnEarlyUpdate()
        {
            m_LocalTime = m_NetworkManager.LocalTime;

            // Starting job during early update will place delta 1 frame behind.
            //if (m_TransformAccessArray.isCreated && m_TransformAccessArray.length > 0)
            if (m_SpawnedInstances.Count > 0)
            {
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
                    //else if (m_TransformsArray != null && m_TransformsArray.Length > 0) 
                    //{
                    //    m_TransformAccessArray.SetTransforms(m_TransformsArray);
                    //    RefreshNativeStates();
                    //}

                    m_CurrentJob = new UpdateTransformStateJob
                    {
                        Current = m_NativeStates,
                        Precision = m_Precision,
                        NextTick = true
                    };

                    m_JobHandle = m_CurrentJob.Schedule(m_TransformAccessArray);
                    m_JobRunning = true;
                }
            }
            //else
            //{
            //    m_TransformAccessArray = new TransformAccessArray(m_Transforms.ToArray());
            //    m_NativeStates = new NativeArray<TransformState>(m_Transforms.Count, Allocator.Persistent);
            //    InitializeNativeStates();
            //}
        }

        private int m_MessageTicketNumber = 0;

        internal void OnPreLateUpdate()
        {
            if (m_LocalTime.Tick != m_LastTickUpdate && m_JobRunning)
            {
                var lastTick = m_LastTickUpdate;
                m_LastTickUpdate = m_LocalTime.Tick;
                // Ensure the job is completed before the next frame
                m_JobHandle.Complete();
                m_JobRunning = false;
                DebugJobResults();
                AvBytesPerUpdate = 0;
                AvHeaderSize = 0;
                AvPayLoadSize = 0;
                if (m_CurrentJob.HasDeltas() > 0)
                {
                    FastBufferWriter.Seek(0);
                    var count = (ushort)0;
                    var tick = m_NetworkManager.LocalTime.Tick;
                    FastBufferWriter.WriteValueSafe(m_MessageTicketNumber);

                    FastBufferWriter.WriteValueSafe(tick);
                    var offset = FastBufferWriter.Position;
                    FastBufferWriter.WriteValueSafe(count);
                    var lastPosition = FastBufferWriter.Position;
                    var internalCount = 0;
                    NetworkLog.LogInfo($"[{nameof(TransformStateManager)}][Send] ======================(BEGIN - {m_MessageTicketNumber})======================");
                    foreach (var entry in m_CurrentJob.Current)
                    {
                        if (entry.GridStateDelta.HasDelta())
                        {
                            entry.GridStateDelta.WriteState(FastBufferWriter);
                            var header = $"[{nameof(TransformStateManager)}][Send][NetworkObjectId: {entry.NetworkObjectId}][Index: {entry.GridStateDelta.Index}][EntityId: {entry.EntityIdentifier}][PayloadSize: {entry.GridStateDelta.Payload_Size}]";
                            if((entry.GridStateDelta.DirtyFlags & 0x02) == 0x02)
                            {
                                header += $"[{entry.GridStateDelta.Position.ToVector3()}]";
                            }

                            if ((entry.GridStateDelta.DirtyFlags & 0x04) == 0x04)
                            {
                                header += $"[{entry.GridStateDelta.Rotation.Rotation}]";
                            }
                            NetworkLog.LogInfo(header);
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
                        NetworkLog.LogInfo($"[{nameof(TransformStateManager)}][Send] ======================(END - NO DATA TO SEND)======================");
                        FastBufferWriter.Seek(0);
                        return;
                    }
                    m_MessageTicketNumber++;
                    var position = FastBufferWriter.Position;
                    FastBufferWriter.Seek(offset);
                    FastBufferWriter.WriteValueSafe(count);
                    FastBufferWriter.Seek(position);

                    var transfromStateUpdateMessage = new TransformStateUpdateMessage()
                    {
                        State = FastBufferWriter.ToArray(),
                    };

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
                        m_NetworkManager.ConnectionManager.SendMessage(ref transfromStateUpdateMessage, NetworkDelivery.ReliableFragmentedSequenced, clients, index);
                    }

                    NetworkLog.LogInfo($"[{nameof(TransformStateManager)}][Send] ======================(END)======================");
                }
            }
        }

        private void DebugJobResults()
        {
#if DEBUGDELTACOMPRESSION
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
#endif
        }

        public int AvBytesPerUpdate;
        public int AvHeaderSize;
        public int AvPayLoadSize;
        internal void UpdateTransformStatesOriginal(FastBufferReader reader)
        {
            var count = (ushort)0;
            var tick = 0;
            var transformState = new TransformIntState();
            try
            {
                transformState.Initialize();
                reader.ReadValueSafe(out tick);
                reader.ReadValueSafe(out count);
                var networkTime = new NetworkTime(m_NetworkManager.NetworkConfig.TickRate, tick);
                var lastPosition = reader.Position;
                for (var i = 0; i < count; i++)
                {
                    transformState.ReadState(reader);
                    if (m_TransformStates.ContainsKey(transformState.NetworkObjectId))
                    {
                        if (m_TransformStates[transformState.NetworkObjectId].ContainsKey(transformState.NetworkBehaviourId))
                        {
                            var readSize = reader.Position - lastPosition;
                            AvBytesPerUpdate = AvBytesPerUpdate == 0 ? +readSize : (int)(0.5f * (AvBytesPerUpdate + readSize));
                            AvHeaderSize = AvHeaderSize == 0 ? +transformState.Header_Size : (int)(0.5f * (AvHeaderSize + transformState.Header_Size));
                            AvPayLoadSize = AvPayLoadSize == 0 ? +transformState.Payload_Size : (int)(0.5f * (AvPayLoadSize + transformState.Payload_Size));
                            transformState.Decompress(m_Precision);
                            m_TransformStates[transformState.NetworkObjectId][transformState.NetworkBehaviourId].UpdateState(networkTime.Time, transformState);
                        }
                        else
                        {
                            NetworkLog.LogErrorServer($"[{nameof(TransformStateManager)}][{nameof(TransformIntState)}] Read an entry for NetworkObjectId-{transformState.NetworkObjectId} : " +
                                $"NetworkBehaviourId-{transformState.NetworkBehaviourId}but it does not exist!");
                        }
                    }
                    else
                    {
                        NetworkLog.LogErrorServer($"[{nameof(TransformStateManager)}][{nameof(TransformIntState)}] Read an entry for NetworkObjectId-{transformState.NetworkObjectId} but it does not exist!");
                    }
                    lastPosition = reader.Position;
                }
            }
            catch (Exception ex)
            {
                transformState.Dispose();
                Debug.LogException(ex);
            }
        }

        internal void UpdateTransformStates(FastBufferReader reader)
        {
            var count = (ushort)0;
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
                reader.ReadValueSafe(out ticketNumber);
                reader.ReadValueSafe(out tick);
                reader.ReadValueSafe(out count);
                var networkTime = new NetworkTime(m_NetworkManager.NetworkConfig.TickRate, tick);
                var lastPosition = reader.Position;
                NetworkLog.LogInfo($"[{nameof(TransformStateManager)}][{ticketNumber}][Receive][Count: {count}");
                for (var i = 0; i < count; i++)
                {
                    transformState.ReadState(reader);

                    var position = (transformState.DirtyFlags & 0x02) == 0x02 ? $"[{transformState.Position.ToVector3()}]" : string.Empty;
                    var rotation = (transformState.DirtyFlags & 0x04) == 0x04 ? $"[{transformState.Rotation.Rotation}]" : string.Empty;
                    NetworkLog.LogInfo($"[Read][NetworkObjectId: {transformState.NetworkObjectId}][NetworkBehaviourId: {transformState.NetworkBehaviourId}]" +
                        $"{position}{rotation}");

                    if (m_TransformStates.ContainsKey(transformState.NetworkObjectId))
                    {
                        if (m_TransformStates[transformState.NetworkObjectId].ContainsKey(transformState.NetworkBehaviourId))
                        {
                            var readSize = reader.Position - lastPosition;
                            AvBytesPerUpdate = AvBytesPerUpdate == 0 ? readSize : (int)(0.5f * (AvBytesPerUpdate + readSize));
                            AvHeaderSize = AvHeaderSize == 0 ? transformState.Header_Size : (int)(0.5f * (AvHeaderSize + transformState.Header_Size));
                            AvPayLoadSize = AvPayLoadSize == 0 ? transformState.Payload_Size : (int)(0.5f * (AvPayLoadSize + transformState.Payload_Size));
                            transformState.Precision = m_Precision;
                            transformState.InvPrecision = 1.0f / m_Precision;
                            transformState.Decompress();

                            var transformStateSync = m_TransformStates[transformState.NetworkObjectId][transformState.NetworkBehaviourId];
                            if (transformStateSync.NetworkObjectId != transformState.NetworkObjectId)
                            {
                                Debug.LogError($"!!!! Trying to update NetworkObjectID: {transformState.NetworkObjectId} but local table points to {transformStateSync.NetworkObjectId}!");
                            }
                            m_TransformStates[transformState.NetworkObjectId][transformState.NetworkBehaviourId].UpdateState(networkTime.Time, transformState);
                        }
                        else
                        {
                            NetworkLog.LogErrorServer($"[{nameof(TransformStateManager)}][{nameof(TransformIntState)}] Read an entry for NetworkObjectId-{transformState.NetworkObjectId} : " +
                                $"NetworkBehaviourId-{transformState.NetworkBehaviourId}but it does not exist!");
                        }
                    }
                    else
                    {
                        NetworkLog.LogErrorServer($"[{nameof(TransformStateManager)}][{nameof(TransformIntState)}] Read an entry for NetworkObjectId-{transformState.NetworkObjectId} but it does not exist!");
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
                state.Delta.Dispose();
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
