using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// A component for syncing transforms.
    /// NetworkTransform will read the underlying transform and replicate it to clients.
    /// The replicated value will be automatically be interpolated (if active) and applied to the underlying GameObject's transform.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Netcode/Network Transform")]
    [DefaultExecutionOrder(100000)] // this is needed to catch the update time after the transform was updated by user scripts
    public class NetworkTransform : NetworkBehaviour
    {
        /// <summary>
        /// The default position change threshold value.
        /// Any changes above this threshold will be replicated.
        /// </summary>
        public const float PositionThresholdDefault = 0.001f;

        /// <summary>
        /// The default rotation angle change threshold value.
        /// Any changes above this threshold will be replicated.
        /// </summary>
        public const float RotAngleThresholdDefault = 0.01f;

        /// <summary>
        /// The default scale change threshold value.
        /// Any changes above this threshold will be replicated.
        /// </summary>
        public const float ScaleThresholdDefault = 0.01f;

        /// <summary>
        /// The handler delegate type that takes client requested changes and returns resulting changes handled by the server.
        /// </summary>
        /// <param name="pos">The position requested by the client.</param>
        /// <param name="rot">The rotation requested by the client.</param>
        /// <param name="scale">The scale requested by the client.</param>
        /// <returns>The resulting position, rotation and scale changes after handling.</returns>
        public delegate (Vector3 pos, Quaternion rotOut, Vector3 scale) OnClientRequestChangeDelegate(Vector3 pos, Quaternion rot, Vector3 scale);

        /// <summary>
        /// The handler that gets invoked when server receives a change from a client.
        /// This handler would be useful for server to modify pos/rot/scale before applying client's request.
        /// </summary>
        public OnClientRequestChangeDelegate OnClientRequestChange;

        public struct NetworkTransformState : INetworkSerializable
        {
            private const int k_InLocalSpaceBit = 0;
            private const int k_PositionXBit = 1;
            private const int k_PositionYBit = 2;
            private const int k_PositionZBit = 3;
            private const int k_RotAngleXBit = 4;
            private const int k_RotAngleYBit = 5;
            private const int k_RotAngleZBit = 6;
            private const int k_ScaleXBit = 7;
            private const int k_ScaleYBit = 8;
            private const int k_ScaleZBit = 9;
            private const int k_TeleportingBit = 10;
            private const int k_Interpolate = 11;
            private const int k_QuaternionSync = 12;
            private const int k_QuaternionCompress = 13;
            private const int k_UseHalfFloats = 14;
            private const int k_Synchronization = 15;
            private const int k_PositionSlerp = 16;

            private uint m_Bitset;

            public bool InLocalSpace
            {
                get => BitGet(k_InLocalSpaceBit);
                set
                {
                    BitSet(value, k_InLocalSpaceBit);
                }
            }

            // Position
            public bool HasPositionX
            {
                get => BitGet(k_PositionXBit);
                set
                {
                    BitSet(value, k_PositionXBit);
                }
            }

            public bool HasPositionY
            {
                get => BitGet(k_PositionYBit);
                set
                {
                    BitSet(value, k_PositionYBit);
                }
            }

            public bool HasPositionZ
            {
                get => BitGet(k_PositionZBit);
                set
                {
                    BitSet(value, k_PositionZBit);
                }
            }

            public bool HasPositionChange
            {
                get
                {
                    return HasPositionX | HasPositionY | HasPositionZ;
                }
            }

            // RotAngles
            public bool HasRotAngleX
            {
                get => BitGet(k_RotAngleXBit);
                set
                {
                    BitSet(value, k_RotAngleXBit);
                }
            }

            public bool HasRotAngleY
            {
                get => BitGet(k_RotAngleYBit);
                set
                {
                    BitSet(value, k_RotAngleYBit);
                }
            }

            public bool HasRotAngleZ
            {
                get => BitGet(k_RotAngleZBit);
                set
                {
                    BitSet(value, k_RotAngleZBit);
                }
            }

            public bool HasRotAngleChange
            {
                get
                {
                    return HasRotAngleX | HasRotAngleY | HasRotAngleZ;
                }
            }

            // Scale
            public bool HasScaleX
            {
                get => BitGet(k_ScaleXBit);
                set
                {
                    BitSet(value, k_ScaleXBit);
                }
            }

            public bool HasScaleY
            {
                get => BitGet(k_ScaleYBit);
                set
                {
                    BitSet(value, k_ScaleYBit);
                }
            }

            public bool HasScaleZ
            {
                get => BitGet(k_ScaleZBit);
                set
                {
                    BitSet(value, k_ScaleZBit);
                }
            }

            public bool HasScaleChange
            {
                get
                {
                    return HasScaleX | HasScaleY | HasScaleZ;
                }
            }

            public bool IsTeleportingNextFrame
            {
                get => BitGet(k_TeleportingBit);
                set
                {
                    BitSet(value, k_TeleportingBit);
                }
            }

            public bool UseInterpolation
            {
                get => BitGet(k_Interpolate);
                set
                {
                    BitSet(value, k_Interpolate);
                }
            }

            public bool QuaternionSync
            {
                get => BitGet(k_QuaternionSync);
                set
                {
                    BitSet(value, k_QuaternionSync);
                }
            }

            public bool QuaternionCompression
            {
                get => BitGet(k_QuaternionCompress);
                set
                {
                    BitSet(value, k_QuaternionCompress);
                }
            }

            public bool UseHalfFloatPrecision
            {
                get => BitGet(k_UseHalfFloats);
                set
                {
                    BitSet(value, k_UseHalfFloats);
                }
            }

            public bool IsSynchronizing
            {
                get => BitGet(k_Synchronization);
                set
                {
                    BitSet(value, k_Synchronization);
                }
            }

            public bool UsePositionSlerp
            {
                get => BitGet(k_PositionSlerp);
                set
                {
                    BitSet(value, k_PositionSlerp);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool BitGet(int bitPosition)
            {
                return (m_Bitset & (1 << bitPosition)) != 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void BitSet(bool set, int bitPosition)
            {
                if (set) { m_Bitset = (m_Bitset | (uint)(1 << bitPosition)); }
                else { m_Bitset = (uint)(m_Bitset & (uint)~(1 << bitPosition)); }
            }

            internal float PositionX, PositionY, PositionZ;
            internal float RotAngleX, RotAngleY, RotAngleZ;
            internal float ScaleX, ScaleY, ScaleZ;
            internal double SentTime;

            // Used for HalfVector3 delta position updates
            internal Vector3 CurrentPosition;
            internal Vector3 DeltaPosition;
            internal HalfVector3 HalfVectorPosition;

            internal HalfVector4 HalfVectorRotation;
            internal uint QuaternionCompressed;

            internal Quaternion Rotation;
            public Quaternion GetRotation()
            {
                return Rotation;
            }

            // Authoritative and non-authoritative sides use this to determine if a NetworkTransformState is
            // dirty or not.
            public bool IsDirty { get; internal set; }

            public int LastSerializedSize;

            // Used for HalfVector3 delta position synchronization
            internal int NetworkTick;

            public int GetNetworkTick()
            {
                return NetworkTick;
            }

            internal bool TrackByStateId;
            internal int StateId;

            /// <summary>
            /// This will reset the NetworkTransform state's internal flags
            /// </summary>
            public void ClearBitSetForNextTick()
            {
                // Preserve the global flags
                var preserveFlags = (uint)((1 << k_InLocalSpaceBit) | (1 << k_Interpolate) | (1 << k_UseHalfFloats) | (1 << k_QuaternionSync) | (1 << k_QuaternionCompress) | (1 << k_PositionSlerp));
                m_Bitset &= preserveFlags;
                IsDirty = false;
            }

            private FastBufferReader m_Reader;
            private FastBufferWriter m_Writer;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                var positionStart = 0;
                var isWriting = serializer.IsWriter;
                if (isWriting)
                {
                    m_Writer = serializer.GetFastBufferWriter();
                    positionStart = m_Writer.Position;
                }
                else
                {
                    m_Reader = serializer.GetFastBufferReader();
                    positionStart = m_Reader.Position;
                }

                if (TrackByStateId)
                {
                    var stateId = StateId;
                    if (IsSynchronizing)
                    {
                        StateId = -1;
                    }
                    else
                    {
                        if (serializer.IsWriter)
                        {
                            StateId++;
                        }
                        serializer.SerializeValue(ref StateId);
                    }
                }

                // Synchronize State Flags and Network Tick
                {
                    if (isWriting)
                    {
                        BytePacker.WriteValueBitPacked(m_Writer, m_Bitset);
                        // We use network ticks as opposed to absolute time as the authoritative
                        // side updates on every new tick.
                        BytePacker.WriteValueBitPacked(m_Writer, NetworkTick);

                    }
                    else
                    {
                        ByteUnpacker.ReadValueBitPacked(m_Reader, out m_Bitset);
                        // We use network ticks as opposed to absolute time as the authoritative
                        // side updates on every new tick.
                        ByteUnpacker.ReadValueBitPacked(m_Reader, out NetworkTick);
                    }
                }

                // Synchronize Position
                if (HasPositionChange)
                {
                    if (UseHalfFloatPrecision)
                    {
                        if (IsTeleportingNextFrame)
                        {
                            // **Always use full precision when teleporting and UseHalfFloatPrecision is enabled**
                            serializer.SerializeValue(ref CurrentPosition);
                            // If we are synchronizing, then include the half vector position's delta offset
                            if (IsSynchronizing)
                            {
                                serializer.SerializeValue(ref DeltaPosition);
                                serializer.SerializeNetworkSerializable(ref HalfVectorPosition);
                            }
                        }
                        else
                        {
                            serializer.SerializeNetworkSerializable(ref HalfVectorPosition);
                        }
                    }
                    else // Legacy Position Synchronization
                    {
                        // Position Values
                        if (HasPositionX)
                        {
                            serializer.SerializeValue(ref PositionX);
                        }

                        if (HasPositionY)
                        {
                            serializer.SerializeValue(ref PositionY);
                        }

                        if (HasPositionZ)
                        {
                            serializer.SerializeValue(ref PositionZ);
                        }
                    }
                }

                // Synchronize Rotation
                if (HasRotAngleChange)
                {
                    if (QuaternionSync)
                    {
                        if (IsTeleportingNextFrame)
                        {
                            serializer.SerializeValue(ref Rotation);
                        }
                        else
                        {
                            if (QuaternionCompression)
                            {
                                if (isWriting)
                                {
                                    QuaternionCompressed = QuaternionCompressor.CompressQuaternion(ref Rotation);
                                }

                                serializer.SerializeValue(ref QuaternionCompressed);

                                if (!isWriting)
                                {
                                    QuaternionCompressor.DecompressQuaternion(ref Rotation, QuaternionCompressed);
                                }
                            }
                            else
                            {
                                if (UseHalfFloatPrecision)
                                {
                                    if (isWriting)
                                    {
                                        HalfVectorRotation.FromQuaternion(ref Rotation);
                                    }

                                    serializer.SerializeNetworkSerializable(ref HalfVectorRotation);

                                    if (!isWriting)
                                    {
                                        HalfVectorRotation.ToQuaternion(ref Rotation);
                                    }
                                }
                                else
                                {
                                    serializer.SerializeValue(ref Rotation);
                                }
                            }
                        }
                    }
                    else
                    {
                        // RotAngle Values
                        if (HasRotAngleX)
                        {
                            serializer.SerializeValue(ref RotAngleX);
                        }

                        if (HasRotAngleY)
                        {
                            serializer.SerializeValue(ref RotAngleY);
                        }

                        if (HasRotAngleZ)
                        {
                            serializer.SerializeValue(ref RotAngleZ);
                        }
                    }
                }

                // Synchronize Scale
                if (HasScaleChange)
                {
                    if (UseHalfFloatPrecision && !IsTeleportingNextFrame)
                    {
                        var halfScale = (ushort)0;
                        if (HasScaleX)
                        {

                            if (isWriting)
                            {
                                halfScale = Mathf.FloatToHalf(ScaleX);
                            }
                            serializer.SerializeValue(ref halfScale);
                            if (!isWriting)
                            {

                                ScaleX = Mathf.HalfToFloat(halfScale);
                            }
                        }

                        if (HasScaleY)
                        {
                            if (isWriting)
                            {
                                halfScale = Mathf.FloatToHalf(ScaleY);
                            }
                            serializer.SerializeValue(ref halfScale);
                            if (!isWriting)
                            {

                                ScaleY = Mathf.HalfToFloat(halfScale);
                            }
                        }

                        if (HasScaleZ)
                        {
                            if (HasScaleY)
                            {
                                if (isWriting)
                                {
                                    halfScale = Mathf.FloatToHalf(ScaleZ);
                                }
                                serializer.SerializeValue(ref halfScale);
                                if (!isWriting)
                                {

                                    ScaleZ = Mathf.HalfToFloat(halfScale);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (HasScaleX)
                        {
                            serializer.SerializeValue(ref ScaleX);
                        }

                        if (HasScaleY)
                        {
                            serializer.SerializeValue(ref ScaleY);
                        }

                        if (HasScaleZ)
                        {
                            serializer.SerializeValue(ref ScaleZ);
                        }
                    }
                }

                // Only if we are receiving state
                if (serializer.IsReader)
                {
                    // Go ahead and mark the local state dirty or not dirty as well
                    /// <see cref="TryCommitTransformToServer"/>
                    IsDirty = HasPositionChange || HasRotAngleChange || HasScaleChange;
                    LastSerializedSize = m_Reader.Position - positionStart;
                }
                else
                {
                    LastSerializedSize = m_Writer.Position - positionStart;
                }
            }
        }

        /// <summary>
        /// Whether or not x component of position will be replicated
        /// </summary>
        public bool SyncPositionX = true;
        /// <summary>
        /// Whether or not y component of position will be replicated
        /// </summary>
        public bool SyncPositionY = true;
        /// <summary>
        /// Whether or not z component of position will be replicated
        /// </summary>
        public bool SyncPositionZ = true;

        private bool SynchronizePosition
        {
            get
            {
                return SyncPositionX || SyncPositionY || SyncPositionZ;
            }
        }

        /// <summary>
        /// Whether or not x component of rotation will be replicated
        /// </summary>
        public bool SyncRotAngleX = true;
        /// <summary>
        /// Whether or not y component of rotation will be replicated
        /// </summary>
        public bool SyncRotAngleY = true;
        /// <summary>
        /// Whether or not z component of rotation will be replicated
        /// </summary>
        public bool SyncRotAngleZ = true;

        private bool SynchronizeRotation
        {
            get
            {
                return SyncRotAngleX || SyncRotAngleY || SyncRotAngleZ;
            }
        }

        /// <summary>
        /// Whether or not x component of scale will be replicated
        /// </summary>
        public bool SyncScaleX = true;
        /// <summary>
        /// Whether or not y component of scale will be replicated
        /// </summary>
        public bool SyncScaleY = true;
        /// <summary>
        /// Whether or not z component of scale will be replicated
        /// </summary>
        public bool SyncScaleZ = true;

        /// <summary>
        /// When set to true and interpolate is enabled, this
        /// will interpolate using Slerp which is useful if your
        /// object's motion is of a circular/Bézier curve like
        /// path.
        ///
        /// !!NOTE!!: Currently this is not synchronized if changed
        /// during runtime!
        ///
        /// TODO: We need to provide a better way to expose more
        /// modularity to interpolation.
        ///
        /// </summary>
        [Tooltip("When enabled the interpolator uses Slerp for circular/Bézier curve motion models.")]
        public bool SlerpPosition = false;

        private bool SynchronizeScale
        {
            get
            {
                return SyncScaleX || SyncScaleY || SyncScaleZ;
            }
        }

        public bool UseQuaternionSynchronization = false;
        public bool UseQuaternionCompression = false;
        public bool UseHalfFloatPrecision = false;

        /// <summary>
        /// The current position threshold value
        /// Any changes to the position that exceeds the current threshold value will be replicated
        /// </summary>
        public float PositionThreshold = PositionThresholdDefault;

        /// <summary>
        /// The current rotation threshold value
        /// Any changes to the rotation that exceeds the current threshold value will be replicated
        /// Minimum Value: 0.001
        /// Maximum Value: 360.0
        /// </summary>
        [Range(0.001f, 360.0f)]
        public float RotAngleThreshold = RotAngleThresholdDefault;

        /// <summary>
        /// The current scale threshold value
        /// Any changes to the scale that exceeds the current threshold value will be replicated
        /// </summary>
        public float ScaleThreshold = ScaleThresholdDefault;

        /// <summary>
        /// Sets whether the transform should be treated as local (true) or world (false) space.
        /// </summary>
        /// <remarks>
        /// This should only be changed by the authoritative side during runtime. Non-authoritative
        /// changes will be overridden upon the next state update.
        /// </remarks>
        [Tooltip("Sets whether this transform should sync in local space or in world space")]
        public bool InLocalSpace = false;

        /// <summary>
        /// When enabled (default) interpolation is applied and when disabled no interpolation is applied
        /// </summary>
        public bool Interpolate = true;

        /// <summary>
        /// Used to determine who can write to this transform. Server only for this transform.
        /// Changing this value alone in a child implementation will not allow you to create a NetworkTransform which can be written to by clients. See the ClientNetworkTransform Sample
        /// in the package samples for how to implement a NetworkTransform with client write support.
        /// If using different values, please use RPCs to write to the server. Netcode doesn't support client side network variable writing
        /// </summary>
        public bool CanCommitToTransform { get; protected set; }


        /// <summary>
        /// Internally used by <see cref="NetworkTransform"/> to keep track of whether this <see cref="NetworkBehaviour"/> derived class instance
        /// was instantiated on the server side or not.
        /// </summary>
        protected bool m_CachedIsServer;

        /// <summary>
        /// Internally used by <see cref="NetworkTransform"/> to keep track of the <see cref="NetworkManager"/> instance assigned to this
        /// this <see cref="NetworkBehaviour"/> derived class instance.
        /// </summary>
        protected NetworkManager m_CachedNetworkManager;

        /// <summary>
        /// We have two internal NetworkVariables.
        /// One for server authoritative and one for "client/owner" authoritative.
        /// </summary>
        private readonly NetworkVariable<NetworkTransformState> m_ReplicatedNetworkStateServer = new NetworkVariable<NetworkTransformState>(new NetworkTransformState(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<NetworkTransformState> m_ReplicatedNetworkStateOwner = new NetworkVariable<NetworkTransformState>(new NetworkTransformState(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        internal NetworkVariable<NetworkTransformState> ReplicatedNetworkState
        {
            get
            {
                if (!IsServerAuthoritative())
                {
                    return m_ReplicatedNetworkStateOwner;
                }

                return m_ReplicatedNetworkStateServer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetSpaceRelativePosition()
        {
            return InLocalSpace ? transform.localPosition : transform.position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion GetSpaceRelativeRotation()
        {
            return InLocalSpace ? transform.localRotation : transform.rotation;
        }

        // Used by both authoritative and non-authoritative instances.
        // This represents the most recent local authoritative state.
        private NetworkTransformState m_LocalAuthoritativeNetworkState;

        private ClientRpcParams m_ClientRpcParams = new ClientRpcParams() { Send = new ClientRpcSendParams() };
        private List<ulong> m_ClientIds = new List<ulong>() { 0 };

        private BufferedLinearInterpolatorVector3 m_BufferedLinearInterpolatorVector3;
        private BufferedLinearInterpolator<Quaternion> m_RotationInterpolator; // rotation is a single Quaternion since each Euler axis will affect the quaternion's final value

        private BufferedLinearInterpolator<float> m_ScaleXInterpolator;
        private BufferedLinearInterpolator<float> m_ScaleYInterpolator;
        private BufferedLinearInterpolator<float> m_ScaleZInterpolator;
        private readonly List<BufferedLinearInterpolator<float>> m_AllFloatInterpolators = new List<BufferedLinearInterpolator<float>>(6);

        // Used by integration test
        internal NetworkTransformState LastSentState;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void UpdatePositionInterpolator(Vector3 position, double time, bool resetInterpolator = false)
        {
            if (!CanCommitToTransform)
            {
                //var position = InLocalSpace ? transform.localPosition : transform.position;
                if (resetInterpolator)
                {
                    m_BufferedLinearInterpolatorVector3.ResetTo(position, time);
                }
                else
                {
                    m_BufferedLinearInterpolatorVector3.AddMeasurement(position, time);
                }
            }
        }

#if DEBUG_NETWORKTRANSFORM
        /// <summary>
        /// For debugging delta position and half vector3
        /// </summary>
        protected delegate void AddLogEntryHandler(ref NetworkTransformState networkTransformState, ulong targetClient, bool preUpdate = false);
        protected AddLogEntryHandler m_AddLogEntry;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddLogEntry(ref NetworkTransformState networkTransformState, ulong targetClient, bool preUpdate = false)
        {
            m_AddLogEntry?.Invoke(ref networkTransformState, targetClient, preUpdate);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int GetStateId(ref NetworkTransformState state)
        {
            return state.StateId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected HalfVector3 GetHalfPositionState()
        {
            return m_HalfPositionState;
        }

#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddLogEntry(ref NetworkTransformState networkTransformState, ulong targetClient, bool preUpdate = false)
        {
        }
#endif




        /// <summary>
        /// Only used when UseHalfFloatPrecision is enabled
        /// </summary>
        private HalfVector3 m_HalfPositionState = new HalfVector3();

        internal void UpdatePositionSlerp()
        {
            if (m_BufferedLinearInterpolatorVector3 != null)
            {
                m_BufferedLinearInterpolatorVector3.IsSlerp = SlerpPosition;
            }
        }

        /// <summary>
        /// Determines if synchronization is needed.
        /// Basically only if we are running in owner authoritative mode and it
        /// is the owner being synchronized we don't want to synchronize with
        /// the exception of the NetworkObject being owned by the server.
        /// </summary>
        private bool ShouldSynchronizeHalfFloat(ulong targetClientId)
        {
            if (!IsServerAuthoritative() && NetworkObject.OwnerClientId == targetClientId)
            {
                // Return false for all client owners but return true for the server
                return NetworkObject.IsOwnedByServer;
            }
            return true;
        }

        /// <summary>
        /// This is invoked when a new client joins (server and client sides)
        /// Server Side: Serializes as if we were teleporting (everything is sent via NetworkTransformState)
        /// Client Side: Adds the interpolated state which applies the NetworkTransformState as well
        /// </summary>
        protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer, ulong targetClientId = 0)
        {
            var synchronizationState = new NetworkTransformState();

            if (serializer.IsWriter)
            {
                synchronizationState.IsTeleportingNextFrame = true;
                var transformToCommit = transform;
                // If we are using Half Float Precision, then we want to only synchronize the authority's m_HalfPositionState.FullPosition in order for
                // for the non-authority side to be able to properly synchronize delta position updates.
                ApplyTransformToNetworkStateWithInfo(ref synchronizationState, ref transformToCommit, true, targetClientId);
                synchronizationState.NetworkSerialize(serializer);
            }
            else
            {
                synchronizationState.NetworkSerialize(serializer);
                // Set the transform's synchronization modes
                InLocalSpace = synchronizationState.InLocalSpace;
                Interpolate = synchronizationState.UseInterpolation;
                UseQuaternionSynchronization = synchronizationState.QuaternionSync;
                UseHalfFloatPrecision = synchronizationState.UseHalfFloatPrecision;
                UseQuaternionCompression = synchronizationState.QuaternionCompression;
                SlerpPosition = synchronizationState.UsePositionSlerp;
                UpdatePositionSlerp();

                // Teleport/Fully Initialize based on the state
                ApplyTeleportingState(synchronizationState);

                m_LocalAuthoritativeNetworkState = synchronizationState;
                m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame = false;
            }
        }

        /// <summary>
        /// This will try to send/commit the current transform delta states (if any)
        /// </summary>
        /// <remarks>
        /// Only client owners or the server should invoke this method
        /// </remarks>
        /// <param name="transformToCommit">the transform to be committed</param>
        /// <param name="dirtyTime">time it was marked dirty</param>
        protected void TryCommitTransformToServer(Transform transformToCommit, double dirtyTime)
        {
            // Only client owners or the server should invoke this method
            if (!IsOwner && !m_CachedIsServer)
            {
                NetworkLog.LogError($"Non-owner instance, {name}, is trying to commit a transform!");
                return;
            }

            // If we are authority, update the authoritative state
            if (CanCommitToTransform)
            {
                OnUpdateAuthoritativeState(ref transformToCommit);
            }
            else // Non-Authority
            {
                var position = GetSpaceRelativePosition();
                var rotation = GetSpaceRelativeRotation();
                // We are an owner requesting to update our state
                if (!m_CachedIsServer)
                {
                    SetStateServerRpc(position, rotation, transformToCommit.localScale, false);
                }
                else // Server is always authoritative (including owner authoritative)
                {
                    SetStateClientRpc(position, rotation, transformToCommit.localScale, false);
                }
            }
        }

        /// <summary>
        /// Invoked just prior to being updated to non-authority instances
        /// </summary>
        /// <param name="networkTransformState">the state being pushed</param>
        protected virtual void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
        {
        }

        /// <summary>
        /// Authoritative side only
        /// If there are any transform delta states, this method will synchronize the
        /// state with all non-authority instances.
        /// </summary>
        private void TryCommitTransform(ref Transform transformToCommit, bool synchronize = false)
        {
            if (!CanCommitToTransform && !IsOwner)
            {
                NetworkLog.LogError($"[{name}] is trying to commit the transform without authority!");
                return;
            }

            // If the transform has deltas (returns dirty) then...
            if (ApplyTransformToNetworkStateWithInfo(ref m_LocalAuthoritativeNetworkState, ref transformToCommit, synchronize))
            {
                m_LocalAuthoritativeNetworkState.LastSerializedSize = ReplicatedNetworkState.Value.LastSerializedSize;
                OnAuthorityPushTransformState(ref m_LocalAuthoritativeNetworkState);

                // "push"/commit the state
                ReplicatedNetworkState.Value = m_LocalAuthoritativeNetworkState;

                // For integration testing
                LastSentState = m_LocalAuthoritativeNetworkState;
            }
        }

        /// <summary>
        /// Initializes the interpolators with the current transform values
        /// </summary>
        private void ResetInterpolatedStateToCurrentAuthoritativeState()
        {
            var serverTime = NetworkManager.ServerTime.Time;
            var position = GetSpaceRelativePosition();

            UpdatePositionInterpolator(position, serverTime, true);
            UpdatePositionSlerp();

            var rotation = GetSpaceRelativeRotation();
            m_RotationInterpolator.ResetTo(rotation, serverTime);

            var scale = transform.localScale;
            m_ScaleXInterpolator.ResetTo(scale.x, serverTime);
            m_ScaleYInterpolator.ResetTo(scale.y, serverTime);
            m_ScaleZInterpolator.ResetTo(scale.z, serverTime);
        }

        /// <summary>
        /// Used for integration testing:
        /// Will apply the transform to the LocalAuthoritativeNetworkState and get detailed dirty information returned
        /// in the <see cref="NetworkTransformState"/> returned.
        /// </summary>
        /// <param name="transform">transform to apply</param>
        /// <returns>NetworkTransformState</returns>
        internal NetworkTransformState ApplyLocalNetworkState(Transform transform)
        {
            // Since we never commit these changes, we need to simulate that any changes were committed previously and the bitset
            // value would already be reset prior to having the state applied
            m_LocalAuthoritativeNetworkState.ClearBitSetForNextTick();

            // Now check the transform for any threshold value changes
            ApplyTransformToNetworkStateWithInfo(ref m_LocalAuthoritativeNetworkState, ref transform);

            // Return the entire state to be used by the integration test
            return m_LocalAuthoritativeNetworkState;
        }

        /// <summary>
        /// Used for integration testing
        /// </summary>
        internal bool ApplyTransformToNetworkState(ref NetworkTransformState networkState, double dirtyTime, Transform transformToUse)
        {
            // Apply the interpolate and PostionDeltaCompression flags, otherwise we get false positives whether something changed or not.
            networkState.UseInterpolation = Interpolate;
            networkState.QuaternionSync = UseQuaternionSynchronization;
            networkState.UseHalfFloatPrecision = UseHalfFloatPrecision;

            return ApplyTransformToNetworkStateWithInfo(ref networkState, ref transformToUse);
        }

        /// <summary>
        /// Applies the transform to the <see cref="NetworkTransformState"/> specified.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ApplyTransformToNetworkStateWithInfo(ref NetworkTransformState networkState, ref Transform transformToUse, bool isSynchronization = false, ulong targetClientId = 0)
        {
            var isDirty = false;
            var isPositionDirty = false;
            var isRotationDirty = false;
            var isScaleDirty = false;

            var position = InLocalSpace ? transformToUse.localPosition : transformToUse.position;
            var rotAngles = InLocalSpace ? transformToUse.localEulerAngles : transformToUse.eulerAngles;
            var scale = transformToUse.localScale;

            networkState.IsSynchronizing = isSynchronization;

            if (InLocalSpace != networkState.InLocalSpace)
            {
                networkState.InLocalSpace = InLocalSpace;
                isDirty = true;
            }

            if (Interpolate != networkState.UseInterpolation)
            {
                networkState.UseInterpolation = Interpolate;
                isDirty = true;
                // When we change from interpolating to not interpolating (or vice versa) we need to synchronize/reset everything
                networkState.IsTeleportingNextFrame = true;
            }

            if (UseQuaternionSynchronization != networkState.QuaternionSync)
            {
                networkState.QuaternionSync = UseQuaternionSynchronization;
                isDirty = true;
                networkState.IsTeleportingNextFrame = true;
            }

            if (UseQuaternionCompression != networkState.QuaternionCompression)
            {
                networkState.QuaternionCompression = UseQuaternionCompression;
                isDirty = true;
                networkState.IsTeleportingNextFrame = true;
            }

            if (UseHalfFloatPrecision != networkState.UseHalfFloatPrecision)
            {
                networkState.UseHalfFloatPrecision = UseHalfFloatPrecision;
                isDirty = true;
                networkState.IsTeleportingNextFrame = true;
            }

            if (SlerpPosition != networkState.UsePositionSlerp)
            {
                networkState.UsePositionSlerp = SlerpPosition;
                isDirty = true;
                networkState.IsTeleportingNextFrame = true;
            }

            if (!UseHalfFloatPrecision)
            {
                if (SyncPositionX && (Mathf.Abs(networkState.PositionX - position.x) >= PositionThreshold || networkState.IsTeleportingNextFrame))
                {
                    networkState.PositionX = position.x;
                    networkState.HasPositionX = true;
                    isPositionDirty = true;
                }

                if (SyncPositionY && (Mathf.Abs(networkState.PositionY - position.y) >= PositionThreshold || networkState.IsTeleportingNextFrame))
                {
                    networkState.PositionY = position.y;
                    networkState.HasPositionY = true;
                    isPositionDirty = true;
                }

                if (SyncPositionZ && (Mathf.Abs(networkState.PositionZ - position.z) >= PositionThreshold || networkState.IsTeleportingNextFrame))
                {
                    networkState.PositionZ = position.z;
                    networkState.HasPositionZ = true;
                    isPositionDirty = true;
                }
            }
            else
            {
                // For HalfVector3, if any axial value is dirty then we always send a full update
                if (!isPositionDirty)
                {
                    var delta = position - m_HalfPositionState.PreviousPosition;
                    for (int i = 0; i < 3; i++)
                    {
                        if (Mathf.Abs(delta[i]) >= PositionThreshold)
                        {
                            isPositionDirty = true;
                            break;
                        }
                    }
                }

                // If the position is dirty or we are teleporting (which includes synchronization)
                // then determine what parts of the HalfVector3 should be updated
                if (isPositionDirty || networkState.IsTeleportingNextFrame)
                {
                    // If we are not synchronizing the transform state for the first time
                    if (!isSynchronization)
                    {
                        // With global teleporting (broadcast to all non-authority instances)
                        // we re-initialize authority's HalfVector3 and synchronize all
                        // non-authority instances with the new full precision position
                        if (networkState.IsTeleportingNextFrame)
                        {
                            m_HalfPositionState = new HalfVector3(position, networkState.NetworkTick);
                            networkState.CurrentPosition = position;
                        }
                        else // Otherwise, just synchronize the delta position value
                        {
                            m_HalfPositionState.FromVector3(ref position, networkState.NetworkTick);
                        }

                        networkState.HalfVectorPosition = m_HalfPositionState;
                    }
                    else // If synchronizing is set, then use the current full position value on the server side
                    {
                        if (ShouldSynchronizeHalfFloat(targetClientId))
                        {
                            // If we have a HalfVector3 that has a state applied, then we want to determine
                            // what needs to be synchronized. For owner authoritative mode, the server side
                            // will have no valid state yet.
                            if (m_HalfPositionState.NetworkTick > 0)
                            {
                                // Always synchronize the base position and the ushort values of the
                                // current m_HalfPositionState
                                networkState.CurrentPosition = m_HalfPositionState.CurrentBasePosition;
                                networkState.HalfVectorPosition = m_HalfPositionState;
                                // If the server is the owner, in both server and owner authoritative modes,
                                // or we are running in server authoritative mode, then we use the
                                // HalfDeltaConvertedBack value as the delta position
                                if (NetworkObject.IsOwnedByServer || IsServerAuthoritative())
                                {
                                    networkState.DeltaPosition = m_HalfPositionState.HalfDeltaConvertedBack;
                                }
                                else
                                {
                                    // Otherwise, we are in owner authoritative mode and the server's HalfVector3
                                    // state is "non-authoritative" relative so we use the DeltaPosition.
                                    networkState.DeltaPosition = m_HalfPositionState.DeltaPosition;
                                }
                            }
                            else // Reset everything and just send the current position
                            {
                                networkState.HalfVectorPosition = new HalfVector3();
                                networkState.DeltaPosition = Vector3.zero;
                                networkState.CurrentPosition = position;
                            }
                        }
                        else
                        {
                            networkState.CurrentPosition = position;
                        }
                        // Add log entry for this update relative to the client being synchronized
                        AddLogEntry(ref networkState, targetClientId, true);
                    }
                    networkState.HasPositionX = true;
                    networkState.HasPositionY = true;
                    networkState.HasPositionZ = true;

                }
            }

            if (!UseQuaternionSynchronization)
            {
                if (SyncRotAngleX && (Mathf.Abs(Mathf.DeltaAngle(networkState.RotAngleX, rotAngles.x)) >= RotAngleThreshold || networkState.IsTeleportingNextFrame))
                {
                    networkState.RotAngleX = rotAngles.x;
                    networkState.HasRotAngleX = true;
                    isRotationDirty = true;
                }

                if (SyncRotAngleY && (Mathf.Abs(Mathf.DeltaAngle(networkState.RotAngleY, rotAngles.y)) >= RotAngleThreshold || networkState.IsTeleportingNextFrame))
                {
                    networkState.RotAngleY = rotAngles.y;
                    networkState.HasRotAngleY = true;
                    isRotationDirty = true;
                }

                if (SyncRotAngleZ && (Mathf.Abs(Mathf.DeltaAngle(networkState.RotAngleZ, rotAngles.z)) >= RotAngleThreshold || networkState.IsTeleportingNextFrame))
                {
                    networkState.RotAngleZ = rotAngles.z;
                    networkState.HasRotAngleZ = true;
                    isRotationDirty = true;
                }
            }
            else
            {
                // For quaternion synchronization, if one angle is dirty we send a full update
                if (!isRotationDirty)
                {
                    var previousRotation = networkState.Rotation.eulerAngles;
                    for (int i = 0; i < 3; i++)
                    {
                        if (Mathf.Abs(Mathf.DeltaAngle(previousRotation[i], rotAngles[i])) >= RotAngleThreshold)
                        {
                            isRotationDirty = true;
                            break;
                        }
                    }
                }
                if (isRotationDirty)
                {
                    networkState.Rotation = InLocalSpace ? transformToUse.localRotation : transformToUse.rotation;
                    networkState.HasRotAngleX = true;
                    networkState.HasRotAngleY = true;
                    networkState.HasRotAngleZ = true;
                }
            }

            // Only if we are not synchronizing...
            if (!isSynchronization)
            {
                if (SyncScaleX && (Mathf.Abs(networkState.ScaleX - scale.x) >= ScaleThreshold || networkState.IsTeleportingNextFrame))
                {
                    networkState.ScaleX = scale.x;
                    networkState.HasScaleX = true;
                    isScaleDirty = true;
                }

                if (SyncScaleY && (Mathf.Abs(networkState.ScaleY - scale.y) >= ScaleThreshold || networkState.IsTeleportingNextFrame))
                {
                    networkState.ScaleY = scale.y;
                    networkState.HasScaleY = true;
                    isScaleDirty = true;
                }

                if (SyncScaleZ && (Mathf.Abs(networkState.ScaleZ - scale.z) >= ScaleThreshold || networkState.IsTeleportingNextFrame))
                {
                    networkState.ScaleZ = scale.z;
                    networkState.HasScaleZ = true;
                    isScaleDirty = true;
                }
            }
            else // If we are synchronizing then use transform's values
            if (isSynchronization)
            {
                networkState.ScaleX = transform.localScale.x;
                networkState.ScaleY = transform.localScale.y;
                networkState.ScaleZ = transform.localScale.z;
                networkState.HasScaleX = true;
                networkState.HasScaleY = true;
                networkState.HasScaleZ = true;
                isScaleDirty = true;
            }
            isDirty |= isPositionDirty || isRotationDirty || isScaleDirty;

            if (isDirty)
            {
                networkState.NetworkTick = NetworkManager.ServerTime.Tick;
            }

            /// We need to set this in order to know when we can reset our local authority state <see cref="Update"/>
            /// If our state is already dirty or we just found deltas (i.e. isDirty == true)
            networkState.IsDirty |= isDirty;
            return isDirty;
        }

        /// <summary>
        /// Applies the authoritative state to the transform
        /// </summary>
        private void ApplyAuthoritativeState()
        {
            var networkState = m_LocalAuthoritativeNetworkState;
            var adjustedPosition = GetSpaceRelativePosition();
            var adjustedRotation = GetSpaceRelativeRotation();
            var adjustedRotAngles = adjustedRotation.eulerAngles;
            var adjustedScale = transform.localScale;

            // Non-Authority Preservers the authority's transform state update modes
            InLocalSpace = networkState.InLocalSpace;
            Interpolate = networkState.UseInterpolation;
            UseHalfFloatPrecision = networkState.UseHalfFloatPrecision;
            UseQuaternionSynchronization = networkState.QuaternionSync;
            UseQuaternionCompression = networkState.QuaternionCompression;
            if (SlerpPosition != networkState.UsePositionSlerp)
            {
                SlerpPosition = networkState.UsePositionSlerp;
                UpdatePositionSlerp();
            }

            // NOTE ABOUT INTERPOLATING AND THE CODE BELOW:
            // We always apply the interpolated state for any axis we are synchronizing even when the state has no deltas
            // to assure we fully interpolate to our target even after we stop extrapolating 1 tick later.
            if (Interpolate)
            {
                var interpolatedPosition = m_BufferedLinearInterpolatorVector3.GetInterpolatedValue();
                if (UseHalfFloatPrecision)
                {
                    adjustedPosition = interpolatedPosition;
                }
                else
                {
                    if (SyncPositionX) { adjustedPosition.x = interpolatedPosition.x; }
                    if (SyncPositionY) { adjustedPosition.y = interpolatedPosition.y; }
                    if (SyncPositionZ) { adjustedPosition.z = interpolatedPosition.z; }
                }

                if (SyncScaleX) { adjustedScale.x = m_ScaleXInterpolator.GetInterpolatedValue(); }
                if (SyncScaleY) { adjustedScale.y = m_ScaleYInterpolator.GetInterpolatedValue(); }
                if (SyncScaleZ) { adjustedScale.z = m_ScaleZInterpolator.GetInterpolatedValue(); }

                if (SynchronizeRotation)
                {
                    var interpolatedRotation = m_RotationInterpolator.GetInterpolatedValue();
                    if (UseQuaternionSynchronization)
                    {
                        adjustedRotation = interpolatedRotation;
                    }
                    else
                    {
                        var interpolatedEulerAngles = interpolatedRotation.eulerAngles;
                        if (SyncRotAngleX) { adjustedRotAngles.x = interpolatedEulerAngles.x; }
                        if (SyncRotAngleY) { adjustedRotAngles.y = interpolatedEulerAngles.y; }
                        if (SyncRotAngleZ) { adjustedRotAngles.z = interpolatedEulerAngles.z; }
                        adjustedRotation.eulerAngles = adjustedRotAngles;
                    }
                }
            }
            else
            {
                if (UseHalfFloatPrecision)
                {
                    adjustedPosition = networkState.CurrentPosition;
                }
                else
                {
                    if (networkState.HasPositionX) { adjustedPosition.x = networkState.PositionX; }
                    if (networkState.HasPositionY) { adjustedPosition.y = networkState.PositionY; }
                    if (networkState.HasPositionZ) { adjustedPosition.z = networkState.PositionZ; }
                }

                if (networkState.HasScaleX) { adjustedScale.x = networkState.ScaleX; }
                if (networkState.HasScaleY) { adjustedScale.y = networkState.ScaleY; }
                if (networkState.HasScaleZ) { adjustedScale.z = networkState.ScaleZ; }

                if (networkState.QuaternionSync)
                {
                    adjustedRotation = networkState.Rotation;
                }
                else
                {
                    if (networkState.HasRotAngleX) { adjustedRotAngles.x = networkState.RotAngleX; }
                    if (networkState.HasRotAngleY) { adjustedRotAngles.y = networkState.RotAngleY; }
                    if (networkState.HasRotAngleZ) { adjustedRotAngles.z = networkState.RotAngleZ; }
                    adjustedRotation.eulerAngles = adjustedRotAngles;
                }
            }

            // NOTE: The below conditional checks for applying axial values are required in order to
            // prevent the non-authoritative side from making adjustments when interpolation is off.

            // TODO: Determine if we want to enforce, frame by frame, the non-authoritative transform values.
            // We would want save the position, rotation, and scale (each individually) after applying each
            // authoritative transform state received. Otherwise, the non-authoritative side could make
            // changes to an axial value (if interpolation is turned off) until authority sends an update for
            // that same axial value. When interpolation is on, the state's values being synchronized are
            // always applied each frame.

            // Apply the new position if it has changed or we are interpolating and synchronizing position
            if (networkState.HasPositionChange || (Interpolate && SynchronizePosition))
            {
                if (InLocalSpace)
                {
                    transform.localPosition = adjustedPosition;
                }
                else
                {
                    transform.position = adjustedPosition;
                }
            }

            // Apply the new rotation if it has changed or we are interpolating and synchronizing rotation
            if (networkState.HasRotAngleChange || (Interpolate && SynchronizeRotation))
            {
                if (InLocalSpace)
                {
                    transform.localRotation = adjustedRotation;
                }
                else
                {
                    transform.rotation = adjustedRotation;
                }
            }

            // Apply the new scale if it has changed or we are interpolating and synchronizing scale
            if (networkState.HasScaleChange || (Interpolate && SynchronizeScale))
            {
                transform.localScale = adjustedScale;
            }
        }

        /// <summary>
        /// Handles applying the full authoritative state (i.e. teleporting)
        /// </summary>
        /// <remarks>
        /// Only non-authoritative instances should invoke this
        /// </remarks>
        private void ApplyTeleportingState(NetworkTransformState newState)
        {
            if (!newState.IsTeleportingNextFrame)
            {
                return;
            }

            var sentTime = newState.SentTime;
            var currentPosition = GetSpaceRelativePosition();
            var currentRotation = GetSpaceRelativeRotation();
            var currentEulerAngles = currentRotation.eulerAngles;
            var currentScale = transform.localScale;

            var isSynchronization = newState.IsSynchronizing;

            // we should clear our float interpolators
            foreach (var interpolator in m_AllFloatInterpolators)
            {
                interpolator.Clear();
            }

            // clear the quaternion interpolator
            m_RotationInterpolator.Clear();

            if (!UseHalfFloatPrecision)
            {
                // Adjust based on which axis changed
                if (newState.HasPositionX)
                {
                    currentPosition.x = newState.PositionX;
                }

                if (newState.HasPositionY)
                {
                    currentPosition.y = newState.PositionY;
                }

                if (newState.HasPositionZ)
                {
                    currentPosition.z = newState.PositionZ;
                }
                UpdatePositionInterpolator(currentPosition, sentTime, true);
            }
            else
            {
                // With half float vector3 delta position teleport updates or synchronization, we
                // create a new instance and provide the current network tick.
                m_HalfPositionState = new HalfVector3(newState.CurrentPosition, newState.NetworkTick);

                // When first synchronizing we determine if we need to apply the current delta position
                // offset or not. This is specific to owner authoritative mode on the owner side only
                if (isSynchronization)
                {
                    if (ShouldSynchronizeHalfFloat(NetworkManager.LocalClientId))
                    {
                        m_HalfPositionState.X = newState.HalfVectorPosition.X;
                        m_HalfPositionState.Y = newState.HalfVectorPosition.Y;
                        m_HalfPositionState.Z = newState.HalfVectorPosition.Z;
                        m_HalfPositionState.DeltaPosition = newState.DeltaPosition;
                        currentPosition = m_HalfPositionState.ToVector3(newState.NetworkTick);
                    }
                    else
                    {
                        currentPosition = newState.CurrentPosition;
                    }
                    // Before the state is applied add a log entry if AddLogEntry is assigned
                    AddLogEntry(ref newState, NetworkObject.OwnerClientId, true);
                }
                else
                {
                    // If we are just teleporting, then we already created a new HalfVector3 value.
                    // set the current position to the state's current position
                    currentPosition = newState.CurrentPosition;
                }

                if (Interpolate)
                {
                    UpdatePositionInterpolator(currentPosition, sentTime);
                }
            }

            // Apply the position
            if (newState.InLocalSpace)
            {
                transform.localPosition = currentPosition;
            }
            else
            {
                transform.position = currentPosition;
            }

            // Adjust based on which axis changed
            if (newState.HasScaleX)
            {
                m_ScaleXInterpolator.ResetTo(newState.ScaleX, sentTime);
                currentScale.x = newState.ScaleX;
            }

            if (newState.HasScaleY)
            {
                m_ScaleYInterpolator.ResetTo(newState.ScaleY, sentTime);
                currentScale.y = newState.ScaleY;
            }

            if (newState.HasScaleZ)
            {
                m_ScaleZInterpolator.ResetTo(newState.ScaleZ, sentTime);
                currentScale.z = newState.ScaleZ;
            }

            // Apply the adjusted scale
            transform.localScale = currentScale;

            if (newState.QuaternionSync && newState.HasRotAngleChange)
            {
                currentRotation = newState.Rotation;
            }
            else
            {
                // Adjust based on which axis changed
                if (newState.HasRotAngleX)
                {
                    currentEulerAngles.x = newState.RotAngleX;
                }

                if (newState.HasRotAngleY)
                {
                    currentEulerAngles.y = newState.RotAngleY;
                }

                if (newState.HasRotAngleZ)
                {
                    currentEulerAngles.z = newState.RotAngleZ;
                }
                currentRotation.eulerAngles = currentEulerAngles;
            }

            if (newState.HasRotAngleChange)
            {
                m_RotationInterpolator.ResetTo(currentRotation, sentTime);
            }

            if (InLocalSpace)
            {
                transform.localRotation = currentRotation;
            }
            else
            {
                transform.rotation = currentRotation;
            }

            // Add log after to applying the update if AddLogEntry is defined
            if (isSynchronization)
            {
                AddLogEntry(ref newState, NetworkObject.OwnerClientId);
            }
        }

        /// <summary>
        /// Adds the new state's values to their respective interpolator
        /// </summary>
        /// <remarks>
        /// Only non-authoritative instances should invoke this
        /// </remarks>
        private void UpdateState(NetworkTransformState oldState, NetworkTransformState newState)
        {
            // Set the transforms's synchronization modes
            InLocalSpace = newState.InLocalSpace;
            Interpolate = newState.UseInterpolation;
            UseQuaternionSynchronization = newState.QuaternionSync;
            UseQuaternionCompression = newState.QuaternionCompression;
            UseHalfFloatPrecision = newState.UseHalfFloatPrecision;
            if (SlerpPosition != newState.UsePositionSlerp)
            {
                SlerpPosition = newState.UsePositionSlerp;
                UpdatePositionSlerp();
            }

            m_LocalAuthoritativeNetworkState = newState;
            if (m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame)
            {
                ApplyTeleportingState(m_LocalAuthoritativeNetworkState);
                return;
            }

            var sentTime = newState.SentTime;
            var currentRotation = GetSpaceRelativeRotation();
            var currentEulerAngles = currentRotation.eulerAngles;


            if (UseHalfFloatPrecision)
            {
                // Since serialization creates a new NetworkTransformState, Non-Authority needs to
                // carry over the HalfVector3 state to the new state's HalfVector3
                m_HalfPositionState.X = m_LocalAuthoritativeNetworkState.HalfVectorPosition.X;
                m_HalfPositionState.Y = m_LocalAuthoritativeNetworkState.HalfVectorPosition.Y;
                m_HalfPositionState.Z = m_LocalAuthoritativeNetworkState.HalfVectorPosition.Z;
                m_LocalAuthoritativeNetworkState.CurrentPosition = m_HalfPositionState.ToVector3(newState.NetworkTick);
            }

            if (!Interpolate)
            {
                return;
            }

            // Apply axial changes from the new state
            // Either apply the delta position target position or the current state's delta position
            // depending upon whether UsePositionDeltaCompression is enabled

            if (m_LocalAuthoritativeNetworkState.HasPositionChange)
            {
                if (m_LocalAuthoritativeNetworkState.UseHalfFloatPrecision)
                {
                    UpdatePositionInterpolator(m_LocalAuthoritativeNetworkState.CurrentPosition, sentTime);
                }
                else
                {
                    var currentPosition = GetSpaceRelativePosition();
                    if (m_LocalAuthoritativeNetworkState.HasPositionX)
                    {
                        currentPosition.x = m_LocalAuthoritativeNetworkState.PositionX;
                    }

                    if (m_LocalAuthoritativeNetworkState.HasPositionY)
                    {
                        currentPosition.y = m_LocalAuthoritativeNetworkState.PositionY;
                    }

                    if (m_LocalAuthoritativeNetworkState.HasPositionZ)
                    {
                        currentPosition.z = m_LocalAuthoritativeNetworkState.PositionZ;
                    }
                    UpdatePositionInterpolator(currentPosition, sentTime);
                }
            }

            if (m_LocalAuthoritativeNetworkState.HasScaleX)
            {
                m_ScaleXInterpolator.AddMeasurement(m_LocalAuthoritativeNetworkState.ScaleX, sentTime);
            }

            if (m_LocalAuthoritativeNetworkState.HasScaleY)
            {
                m_ScaleYInterpolator.AddMeasurement(m_LocalAuthoritativeNetworkState.ScaleY, sentTime);
            }

            if (m_LocalAuthoritativeNetworkState.HasScaleZ)
            {
                m_ScaleZInterpolator.AddMeasurement(m_LocalAuthoritativeNetworkState.ScaleZ, sentTime);
            }

            // With rotation, we check if there are any changes first and
            // if so then apply the changes to the current Euler rotation
            // values.
            if (m_LocalAuthoritativeNetworkState.HasRotAngleChange)
            {
                if (m_LocalAuthoritativeNetworkState.QuaternionSync)
                {
                    currentRotation = m_LocalAuthoritativeNetworkState.Rotation;
                }
                else
                {
                    // Adjust based on which axis changed
                    if (m_LocalAuthoritativeNetworkState.HasRotAngleX)
                    {
                        currentEulerAngles.x = m_LocalAuthoritativeNetworkState.RotAngleX;
                    }

                    if (m_LocalAuthoritativeNetworkState.HasRotAngleY)
                    {
                        currentEulerAngles.y = m_LocalAuthoritativeNetworkState.RotAngleY;
                    }

                    if (m_LocalAuthoritativeNetworkState.HasRotAngleZ)
                    {
                        currentEulerAngles.z = m_LocalAuthoritativeNetworkState.RotAngleZ;
                    }
                    currentRotation.eulerAngles = currentEulerAngles;
                }

                m_RotationInterpolator.AddMeasurement(currentRotation, sentTime);
            }
        }

        /// <summary>
        /// Invoked on the non-authoritative side when the NetworkTransformState has been updated
        /// </summary>
        /// <param name="oldState">the previous <see cref="NetworkTransformState"/></param>
        /// <param name="newState">the new <see cref="NetworkTransformState"/></param>
        protected virtual void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
        {

        }

        /// <summary>
        /// Only non-authoritative instances should invoke this method
        /// </summary>
        private void OnNetworkStateChanged(NetworkTransformState oldState, NetworkTransformState newState)
        {
            if (!NetworkObject.IsSpawned || CanCommitToTransform)
            {
                return;
            }

            // Get the time when this new state was sent
            newState.SentTime = new NetworkTime(NetworkManager.NetworkConfig.TickRate, newState.NetworkTick).Time;

            // Update the state
            UpdateState(oldState, newState);

            // Provide notifications when the state has been updated
            OnNetworkTransformStateUpdated(ref oldState, ref newState);
        }

        /// <summary>
        /// Will set the maximum interpolation boundary for the interpolators of this <see cref="NetworkTransform"/> instance.
        /// This value roughly translates to the maximum value of 't' in <see cref="Mathf.Lerp(float, float, float)"/> and
        /// <see cref="Mathf.LerpUnclamped(float, float, float)"/> for all transform elements being monitored by
        /// <see cref="NetworkTransform"/> (i.e. Position, Rotation, and Scale)
        /// </summary>
        /// <param name="maxInterpolationBound">Maximum time boundary that can be used in a frame when interpolating between two values</param>
        public void SetMaxInterpolationBound(float maxInterpolationBound)
        {
            m_RotationInterpolator.MaxInterpolationBound = maxInterpolationBound;
            m_ScaleXInterpolator.MaxInterpolationBound = maxInterpolationBound;
            m_ScaleYInterpolator.MaxInterpolationBound = maxInterpolationBound;
            m_ScaleZInterpolator.MaxInterpolationBound = maxInterpolationBound;
            m_BufferedLinearInterpolatorVector3.MaxInterpolationBound = maxInterpolationBound;
        }

        /// <summary>
        /// Create interpolators when first instantiated to avoid memory allocations if the
        /// associated NetworkObject persists (i.e. despawned but not destroyed or pools)
        /// </summary>
        protected virtual void Awake()
        {
            // Rotation is a single Quaternion since each Euler axis will affect the quaternion's final value
            m_RotationInterpolator = new BufferedLinearInterpolatorQuaternion();
            m_BufferedLinearInterpolatorVector3 = new BufferedLinearInterpolatorVector3();

            // All other interpolators are BufferedLinearInterpolatorFloats
            m_ScaleXInterpolator = new BufferedLinearInterpolatorFloat();
            m_ScaleYInterpolator = new BufferedLinearInterpolatorFloat();
            m_ScaleZInterpolator = new BufferedLinearInterpolatorFloat();

            // Used to quickly iteration over the BufferedLinearInterpolatorFloat
            // instances
            if (m_AllFloatInterpolators.Count == 0)
            {
                m_AllFloatInterpolators.Add(m_ScaleXInterpolator);
                m_AllFloatInterpolators.Add(m_ScaleYInterpolator);
                m_AllFloatInterpolators.Add(m_ScaleZInterpolator);
            }
        }

        /// <summary>
        /// Can be overridden to customize what is updated or make adjustments to the
        /// transform prior to pushing the updates to non-authoritative instances.
        /// </summary>
        /// <remarks>
        /// !!! NOTE !!!
        /// This also reset the m_LocalAuthoritativeNetworkState if it is still dirty
        /// but the replicated network state is not.
        /// </remarks>
        /// <param name="transformSource">transform to be updated</param>
        protected virtual void OnUpdateAuthoritativeState(ref Transform transformSource)
        {
            // If our replicated state is not dirty and our local authority state is dirty, clear it.
            if (!ReplicatedNetworkState.IsDirty() && m_LocalAuthoritativeNetworkState.IsDirty)
            {
                // Now clear our bitset and prepare for next network tick state update
                m_LocalAuthoritativeNetworkState.ClearBitSetForNextTick();
            }
            TryCommitTransform(ref transformSource);
        }

        /// <summary>
        /// Authority subscribes to network tick events and will invoke
        /// <see cref="OnUpdateAuthoritativeState(ref Transform)"/> each
        /// network tick.
        /// </summary>
        private void NetworkTickSystem_Tick()
        {
            // As long as we are still authority
            if (CanCommitToTransform)
            {
                // Update any changes to the transform
                var transformSource = transform;
                OnUpdateAuthoritativeState(ref transformSource);
            }
            else
            {
                // If we are no longer authority, unsubscribe to the tick event
                if (NetworkManager != null && NetworkManager.NetworkTickSystem != null)
                {
                    NetworkManager.NetworkTickSystem.Tick -= NetworkTickSystem_Tick;
                }
            }
        }

        /// <inheritdoc/>
        public override void OnNetworkSpawn()
        {
            m_CachedIsServer = IsServer;
            m_CachedNetworkManager = NetworkManager;

            Initialize();
            // This assures the initial spawning of the object synchronizes all connected clients
            // with the current transform values. This should not be placed within Initialize since
            // that can be invoked when ownership changes.
            if (CanCommitToTransform)
            {
                var currentPosition = GetSpaceRelativePosition();
                var currentRotation = GetSpaceRelativeRotation();
                // Teleport to current position
                SetStateInternal(currentPosition, currentRotation, transform.localScale, true);

                // Force the state update to be sent
                var transformToCommit = transform;
                TryCommitTransform(ref transformToCommit);
            }
        }

        /// <inheritdoc/>
        public override void OnNetworkDespawn()
        {
            ReplicatedNetworkState.OnValueChanged -= OnNetworkStateChanged;
            CanCommitToTransform = false;
            if (NetworkManager != null && NetworkManager.NetworkTickSystem != null)
            {
                NetworkManager.NetworkTickSystem.Tick -= NetworkTickSystem_Tick;
            }
        }

        /// <inheritdoc/>
        public override void OnDestroy()
        {
            if (NetworkManager != null && NetworkManager.NetworkTickSystem != null)
            {
                NetworkManager.NetworkTickSystem.Tick -= NetworkTickSystem_Tick;
            }
            CanCommitToTransform = false;
            base.OnDestroy();
            m_ReplicatedNetworkStateServer.Dispose();
            m_ReplicatedNetworkStateOwner.Dispose();

        }

        /// <inheritdoc/>
        public override void OnGainedOwnership()
        {
            // Only initialize if we gained ownership
            if (OwnerClientId == NetworkManager.LocalClientId)
            {
                Initialize();
            }
        }

        /// <inheritdoc/>
        public override void OnLostOwnership()
        {
            // Only initialize if we are not authority and lost
            // ownership
            if (OwnerClientId != NetworkManager.LocalClientId)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Invoked when first spawned as well as when ownership changes
        /// </summary>
        /// <param name="replicatedState">the <see cref="NetworkVariable{T}"/> replicated <see cref="NetworkTransformState"/></param>
        protected virtual void OnInitialize(ref NetworkVariable<NetworkTransformState> replicatedState)
        {

        }

        /// <summary>
        /// Initializes NetworkTransform when spawned and ownership changes.
        /// </summary>
        protected void Initialize()
        {
            if (!IsSpawned)
            {
                return;
            }

            CanCommitToTransform = IsServerAuthoritative() ? IsServer : IsOwner;
            var replicatedState = ReplicatedNetworkState;
            var currentPosition = GetSpaceRelativePosition();

            if (CanCommitToTransform)
            {
                if (UseHalfFloatPrecision)
                {
                    m_HalfPositionState = new HalfVector3(currentPosition, NetworkManager.NetworkTickSystem.ServerTime.Tick);
                }

                // Authority only updates once per network tick
                NetworkManager.NetworkTickSystem.Tick -= NetworkTickSystem_Tick;
                NetworkManager.NetworkTickSystem.Tick += NetworkTickSystem_Tick;
            }
            else
            {
                // Sanity check to assure we only subscribe to OnValueChanged once
                replicatedState.OnValueChanged -= OnNetworkStateChanged;
                replicatedState.OnValueChanged += OnNetworkStateChanged;

                // Assure we no longer subscribe to the tick event
                NetworkManager.NetworkTickSystem.Tick -= NetworkTickSystem_Tick;

                ResetInterpolatedStateToCurrentAuthoritativeState();
            }

            OnInitialize(ref replicatedState);
        }

        /// <summary>
        /// Directly sets a state on the authoritative transform.
        /// Owner clients can directly set the state on a server authoritative transform
        /// This will override any changes made previously to the transform
        /// This isn't resistant to network jitter. Server side changes due to this method won't be interpolated.
        /// The parameters are broken up into pos / rot / scale on purpose so that the caller can perturb
        ///  just the desired one(s)
        /// </summary>
        /// <param name="posIn"></param> new position to move to.  Can be null
        /// <param name="rotIn"></param> new rotation to rotate to.  Can be null
        /// <param name="scaleIn">new scale to scale to. Can be null</param>
        /// <param name="teleportDisabled">When true (the default) the <see cref="NetworkObject"/> will not be teleported and, if enabled, will interpolate. When false the <see cref="NetworkObject"/> will teleport/apply the parameters provided immediately.</param>
        /// <exception cref="Exception"></exception>
        public void SetState(Vector3? posIn = null, Quaternion? rotIn = null, Vector3? scaleIn = null, bool teleportDisabled = true)
        {
            if (!IsSpawned)
            {
                return;
            }

            // Only the server or owner can invoke this method
            if (!IsOwner && !m_CachedIsServer)
            {
                throw new Exception("Non-owner client instance cannot set the state of the NetworkTransform!");
            }

            Vector3 pos = posIn == null ? GetSpaceRelativePosition() : posIn.Value;
            Quaternion rot = rotIn == null ? GetSpaceRelativeRotation() : rotIn.Value;
            Vector3 scale = scaleIn == null ? transform.localScale : scaleIn.Value;

            if (!CanCommitToTransform)
            {
                // Preserving the ability for owner authoritative mode to accept state changes from server
                if (m_CachedIsServer)
                {
                    m_ClientIds[0] = OwnerClientId;
                    m_ClientRpcParams.Send.TargetClientIds = m_ClientIds;
                    SetStateClientRpc(pos, rot, scale, !teleportDisabled, m_ClientRpcParams);
                }
                else // Preserving the ability for server authoritative mode to accept state changes from owner
                {
                    SetStateServerRpc(pos, rot, scale, !teleportDisabled);
                }
                return;
            }

            SetStateInternal(pos, rot, scale, !teleportDisabled);
        }

        /// <summary>
        /// Authoritative only method
        /// Sets the internal state (teleporting or just set state) of the authoritative
        /// transform directly.
        /// </summary>
        private void SetStateInternal(Vector3 pos, Quaternion rot, Vector3 scale, bool shouldTeleport)
        {
            if (InLocalSpace)
            {
                transform.localPosition = pos;
                transform.localRotation = rot;
            }
            else
            {
                transform.SetPositionAndRotation(pos, rot);
            }
            transform.localScale = scale;
            m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame = shouldTeleport;
            var transformToCommit = transform;
            TryCommitTransform(ref transformToCommit);
        }

        /// <summary>
        /// Invoked by <see cref="SetState"/>, allows a non-owner server to update the transform state
        /// </summary>
        /// <remarks>
        /// Continued support for client-driven server authority model
        /// </remarks>
        [ClientRpc]
        private void SetStateClientRpc(Vector3 pos, Quaternion rot, Vector3 scale, bool shouldTeleport, ClientRpcParams clientRpcParams = default)
        {
            // Server dictated state is always applied
            SetStateInternal(pos, rot, scale, shouldTeleport);
        }

        /// <summary>
        /// Invoked by <see cref="SetState"/>, allows an owner-client update the transform state
        /// </summary>
        /// <remarks>
        /// Continued support for client-driven server authority model
        /// </remarks>
        [ServerRpc]
        private void SetStateServerRpc(Vector3 pos, Quaternion rot, Vector3 scale, bool shouldTeleport)
        {
            // server has received this RPC request to move change transform. give the server a chance to modify or even reject the move
            if (OnClientRequestChange != null)
            {
                (pos, rot, scale) = OnClientRequestChange(pos, rot, scale);
            }
            SetStateInternal(pos, rot, scale, shouldTeleport);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// If you override this method, be sure that:
        /// - Non-authority always invokes this base class method when using interpolation.
        /// </remarks>
        protected virtual void Update()
        {
            // If not spawned or this instance has authority, exit early
            if (!IsSpawned || CanCommitToTransform)
            {
                return;
            }

            // Non-Authority
            if (Interpolate)
            {
                var serverTime = NetworkManager.ServerTime;
                var cachedDeltaTime = Time.deltaTime;
                var cachedServerTime = serverTime.Time;
                // TODO: Investigate Further
                // With owner authoritative mode, non-authority clients can lag behind
                // by more than 1 tick period of time. The current "solution" for now
                // is to make their cachedRenderTime run 2 ticks behind.
                var ticksAgo = !IsServerAuthoritative() && !IsServer ? 2 : 1;
                var cachedRenderTime = serverTime.TimeTicksAgo(ticksAgo).Time;
                foreach (var interpolator in m_AllFloatInterpolators)
                {
                    interpolator.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);
                }

                m_BufferedLinearInterpolatorVector3.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);
                m_RotationInterpolator.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);
            }

            // Apply the current authoritative state
            ApplyAuthoritativeState();
        }

        /// <summary>
        /// Teleport the transform to the given values without interpolating
        /// </summary>
        /// <param name="newPosition"></param> new position to move to.
        /// <param name="newRotation"></param> new rotation to rotate to.
        /// <param name="newScale">new scale to scale to.</param>
        /// <exception cref="Exception"></exception>
        public void Teleport(Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
        {
            if (!CanCommitToTransform)
            {
                throw new Exception("Teleporting on non-authoritative side is not allowed!");
            }

            // Teleporting now is as simple as setting the internal state and passing the teleport flag
            SetStateInternal(newPosition, newRotation, newScale, true);
        }

        /// <summary>
        /// Override this method and return false to switch to owner authoritative mode
        /// </summary>
        /// <returns>(<see cref="true"/> or <see cref="false"/>) where when false it runs as owner-client authoritative</returns>
        protected virtual bool OnIsServerAuthoritative()
        {
            return true;
        }

        /// <summary>
        /// Used by <see cref="NetworkRigidbody"/> to determines if this is server or owner authoritative.
        /// </summary>
        public bool IsServerAuthoritative()
        {
            return OnIsServerAuthoritative();
        }
    }


    internal interface INetworkTransformLogStateEntry
    {
        void AddLogEntry(NetworkTransform.NetworkTransformState networkTransformState, ulong targetClient, bool preUpdate = false);
    }
}

