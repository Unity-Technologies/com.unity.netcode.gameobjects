using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
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

        /// <summary>
        /// Data structure used to synchronize the <see cref="NetworkTransform"/>
        /// </summary>
        public struct NetworkTransformState : INetworkSerializable
        {
            private const int k_InLocalSpaceBit = 0x00000001; // Persists between state updates (authority dictates if this is set)
            private const int k_PositionXBit = 0x00000002;
            private const int k_PositionYBit = 0x00000004;
            private const int k_PositionZBit = 0x00000008;
            private const int k_RotAngleXBit = 0x00000010;
            private const int k_RotAngleYBit = 0x00000020;
            private const int k_RotAngleZBit = 0x00000040;
            private const int k_ScaleXBit = 0x00000080;
            private const int k_ScaleYBit = 0x00000100;
            private const int k_ScaleZBit = 0x00000200;
            private const int k_TeleportingBit = 0x00000400;
            private const int k_Interpolate = 0x00000800; // Persists between state updates (authority dictates if this is set)
            private const int k_QuaternionSync = 0x00001000; // Persists between state updates (authority dictates if this is set)
            private const int k_QuaternionCompress = 0x00002000; // Persists between state updates (authority dictates if this is set)
            private const int k_UseHalfFloats = 0x00004000; // Persists between state updates (authority dictates if this is set)
            private const int k_Synchronization = 0x00008000;
            private const int k_PositionSlerp = 0x00010000; // Persists between state updates (authority dictates if this is set)

            // Stores persistent and state relative flags
            private uint m_Bitset;

            // Used to store the tick calculated sent time
            internal double SentTime;

            // Used for full precision position updates
            internal float PositionX, PositionY, PositionZ;

            // Used for full precision Euler updates
            internal float RotAngleX, RotAngleY, RotAngleZ;

            // Used for full precision quaternion updates
            internal Quaternion Rotation;

            // Used for full precision scale updates
            internal float ScaleX, ScaleY, ScaleZ;

            // Used for half precision delta position updates
            internal Vector3 CurrentPosition;
            internal Vector3 DeltaPosition;
            internal NetworkDeltaPosition NetworkDeltaPosition;

            // Used for half precision scale
            internal HalfVector3 HalfVectorScale;
            internal Vector3 Scale;

            // Used for half precision quaternion
            internal HalfVector4 HalfVectorRotation;

            // Used to store a compressed quaternion
            internal uint QuaternionCompressed;

            // Authoritative and non-authoritative sides use this to determine if a NetworkTransformState is
            // dirty or not.
            internal bool IsDirty { get; set; }

            /// <summary>
            /// The last byte size of the <see cref="NetworkTransformState"/> updated.
            /// </summary>
            public int LastSerializedSize { get; internal set; }

            // Used for NetworkDeltaPosition delta position synchronization
            internal int NetworkTick;

            // Used when tracking by state ID is enabled
            internal bool TrackByStateId;
            internal int StateId;

            // Used during serialization
            private FastBufferReader m_Reader;
            private FastBufferWriter m_Writer;

            /// <summary>
            /// When set, the <see cref="NetworkTransform"/> is operates in local space
            /// </summary>
            public bool InLocalSpace
            {
                get => GetFlag(k_InLocalSpaceBit);
                internal set
                {
                    SetFlag(value, k_InLocalSpaceBit);
                }
            }

            // Position
            /// <summary>
            /// When set, the X-Axis position value has changed
            /// </summary>
            public bool HasPositionX
            {
                get => GetFlag(k_PositionXBit);
                internal set
                {
                    SetFlag(value, k_PositionXBit);
                }
            }

            /// <summary>
            /// When set, the Y-Axis position value has changed
            /// </summary>
            public bool HasPositionY
            {
                get => GetFlag(k_PositionYBit);
                internal set
                {
                    SetFlag(value, k_PositionYBit);
                }
            }

            /// <summary>
            /// When set, the Z-Axis position value has changed
            /// </summary>
            public bool HasPositionZ
            {
                get => GetFlag(k_PositionZBit);
                internal set
                {
                    SetFlag(value, k_PositionZBit);
                }
            }

            /// <summary>
            /// When set, at least one of the position axis values has changed.
            /// </summary>
            public bool HasPositionChange
            {
                get
                {
                    return HasPositionX | HasPositionY | HasPositionZ;
                }
            }

            // RotAngles
            /// <summary>
            /// When set, the Euler rotation X-Axis value has changed.
            /// </summary>
            /// <remarks>
            /// When quaternion synchronization is enabled all axis are always updated.
            /// </remarks>
            public bool HasRotAngleX
            {
                get => GetFlag(k_RotAngleXBit);
                internal set
                {
                    SetFlag(value, k_RotAngleXBit);
                }
            }

            /// <summary>
            /// When set, the Euler rotation Y-Axis value has changed.
            /// </summary>
            /// <remarks>
            /// When quaternion synchronization is enabled all axis are always updated.
            /// </remarks>
            public bool HasRotAngleY
            {
                get => GetFlag(k_RotAngleYBit);
                internal set
                {
                    SetFlag(value, k_RotAngleYBit);
                }
            }

            /// <summary>
            /// When set, the Euler rotation Z-Axis value has changed.
            /// </summary>
            /// <remarks>
            /// When quaternion synchronization is enabled all axis are always updated.
            /// </remarks>
            public bool HasRotAngleZ
            {
                get => GetFlag(k_RotAngleZBit);
                internal set
                {
                    SetFlag(value, k_RotAngleZBit);
                }
            }

            /// <summary>
            /// When set, at least one of the rotation axis values has changed.
            /// </summary>
            /// <remarks>
            /// When quaternion synchronization is enabled all axis are always updated.
            /// </remarks>
            public bool HasRotAngleChange
            {
                get
                {
                    return HasRotAngleX | HasRotAngleY | HasRotAngleZ;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool HasScale(int axisIndex)
            {
                return GetFlag(k_ScaleXBit << axisIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void SetHasScale(int axisIndex, bool isSet)
            {
                SetFlag(isSet, k_ScaleXBit << axisIndex);
            }

            // Scale
            /// <summary>
            /// When set, the X-Axis scale value has changed.
            /// </summary>
            public bool HasScaleX
            {
                get => GetFlag(k_ScaleXBit);
                internal set
                {
                    SetFlag(value, k_ScaleXBit);
                }
            }

            /// <summary>
            /// When set, the Y-Axis scale value has changed.
            /// </summary>
            public bool HasScaleY
            {
                get => GetFlag(k_ScaleYBit);
                internal set
                {
                    SetFlag(value, k_ScaleYBit);
                }
            }

            /// <summary>
            /// When set, the Z-Axis scale value has changed.
            /// </summary>
            public bool HasScaleZ
            {
                get => GetFlag(k_ScaleZBit);
                internal set
                {
                    SetFlag(value, k_ScaleZBit);
                }
            }

            /// <summary>
            /// When set, at least one of the scale axis values has changed.
            /// </summary>
            public bool HasScaleChange
            {
                get
                {
                    return HasScaleX | HasScaleY | HasScaleZ;
                }
            }

            /// <summary>
            /// When set, the current state will be treated as a teleport.
            /// </summary>
            /// <remarks>
            /// When teleporting:
            /// - Interpolation is reset.
            /// - If using half precision, full precision values are used.
            /// - All axis marked to be synchronized will be updated.
            /// </remarks>
            public bool IsTeleportingNextFrame
            {
                get => GetFlag(k_TeleportingBit);
                internal set
                {
                    SetFlag(value, k_TeleportingBit);
                }
            }

            /// <summary>
            /// When set the <see cref="NetworkTransform"/> is uses interpolation.
            /// </summary>
            /// <remarks>
            /// Authority does not apply interpolation via <see cref="NetworkTransform"/>.
            /// Authority should handle its own motion/rotation/scale smoothing locally.
            /// </remarks>
            public bool UseInterpolation
            {
                get => GetFlag(k_Interpolate);
                internal set
                {
                    SetFlag(value, k_Interpolate);
                }
            }

            /// <summary>
            /// When enabled, this <see cref="NetworkTransform"/> instance uses <see cref="Quaternion"/> synchronization.
            /// </summary>
            /// <remarks>
            /// Use quaternion synchronization if you are nesting <see cref="NetworkTransform"/>s and rotation can occur on both the parent and child.
            /// When quaternion synchronization is enabled, the entire quaternion is updated when there are any changes to any axial values.
            /// You can use half float precision or quaternion compression to reduce the bandwidth cost.
            /// </remarks>
            public bool QuaternionSync
            {
                get => GetFlag(k_QuaternionSync);
                internal set
                {
                    SetFlag(value, k_QuaternionSync);
                }
            }

            /// <summary>
            /// When set <see cref="Quaternion"/>s will be compressed down to 4 bytes using a smallest three implementation.
            /// </summary>
            /// <remarks>
            /// This only will be applied when <see cref="QuaternionSync"/> is enabled.
            /// Half float precision provides a higher precision than quaternion compression but at the cost of 4 additional bytes per update.
            /// - Quaternion Compression: 4 bytes per delta update
            /// - Half float precision: 8 bytes per delta update
            /// </remarks>
            public bool QuaternionCompression
            {
                get => GetFlag(k_QuaternionCompress);
                internal set
                {
                    SetFlag(value, k_QuaternionCompress);
                }
            }

            /// <summary>
            /// When set, the <see cref="NetworkTransform"/> will use half float precision for position, rotation, and scale.
            /// </summary>
            /// <remarks>
            /// Postion is synchronized through delta position updates in order to reduce precision loss/drift and to extend to positions beyond the limitation of half float maximum values.
            /// Rotation and scale both use half float precision (<see cref="HalfVector4"/> and <see cref="HalfVector3"/>)
            /// </remarks>
            public bool UseHalfFloatPrecision
            {
                get => GetFlag(k_UseHalfFloats);
                internal set
                {
                    SetFlag(value, k_UseHalfFloats);
                }
            }

            /// <summary>
            /// When set, this indicates it is the first state being synchronized.
            /// Typically when the associate <see cref="NetworkObject"/> is spawned or a client is being synchronized after connecting to a network session in progress.
            /// </summary>
            public bool IsSynchronizing
            {
                get => GetFlag(k_Synchronization);
                internal set
                {
                    SetFlag(value, k_Synchronization);
                }
            }

            /// <summary>
            /// Determines if position interpolation will Slerp towards its target position.
            /// This is only really useful if you are moving around a point in a circular pattern.
            /// </summary>
            public bool UsePositionSlerp
            {
                get => GetFlag(k_PositionSlerp);
                internal set
                {
                    SetFlag(value, k_PositionSlerp);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool GetFlag(int flag)
            {
                return (m_Bitset & flag) != 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetFlag(bool set, int flag)
            {
                if (set) { m_Bitset = m_Bitset | (uint)flag; }
                else { m_Bitset = m_Bitset & (uint)~flag; }
            }

            internal void ClearBitSetForNextTick()
            {
                // Clear everything but flags that should persist between state updates until changed by authority
                m_Bitset &= k_InLocalSpaceBit | k_Interpolate | k_UseHalfFloats | k_QuaternionSync | k_QuaternionCompress | k_PositionSlerp;
                IsDirty = false;
            }

            /// <summary>
            /// Returns the current state's rotation. If there is no change in the rotation,
            /// then it will return <see cref="Quaternion.identity"/>.
            /// </summary>
            /// <remarks>
            /// When there is no change in an updated state's rotation then there are no values to return.
            /// Checking for <see cref="HasRotAngleChange"/> is one way to detect this.
            /// </remarks>
            /// <returns><see cref="Quaternion"/></returns>
            public Quaternion GetRotation()
            {
                if (HasRotAngleChange)
                {
                    if (QuaternionSync)
                    {
                        return Rotation;
                    }
                    else
                    {
                        return Quaternion.Euler(RotAngleX, RotAngleY, RotAngleZ);
                    }
                }
                return Quaternion.identity;
            }

            /// <summary>
            /// Returns the current state's position. If there is no change in position,
            /// then it returns <see cref="Vector3.zero"/>.
            /// </summary>
            /// <remarks>
            /// When there is no change in an updated state's position then there are no values to return.
            /// Checking for <see cref="HasPositionChange"/> is one way to detect this.
            /// </remarks>
            /// <returns><see cref="Vector3"/></returns>
            public Vector3 GetPosition()
            {
                if (HasPositionChange)
                {
                    if (UseHalfFloatPrecision)
                    {
                        if (IsTeleportingNextFrame)
                        {
                            return CurrentPosition;
                        }
                        else
                        {
                            return NetworkDeltaPosition.GetFullPosition();
                        }
                    }
                    else
                    {
                        return new Vector3(PositionX, PositionY, PositionZ);
                    }
                }
                return Vector3.zero;
            }

            /// <summary>
            /// Returns the current state's scale. If there is no change in scale,
            /// then it returns <see cref="Vector3.zero"/>.
            /// </summary>
            /// <remarks>
            /// When there is no change in an updated state's scale then there are no values to return.
            /// Checking for <see cref="HasScaleChange"/> is one way to detect this.
            /// </remarks>
            /// <returns><see cref="Vector3"/></returns>
            public Vector3 GetScale()
            {
                if (HasScaleChange)
                {
                    if (UseHalfFloatPrecision)
                    {
                        if (IsTeleportingNextFrame)
                        {
                            return Scale;
                        }
                        else
                        {
                            return HalfVectorScale.ToVector3();
                        }
                    }
                    else
                    {
                        return new Vector3(ScaleX, ScaleY, ScaleZ);
                    }
                }
                return Vector3.zero;
            }

            /// <summary>
            /// The network tick that this state was sent by the authoritative instance.
            /// </summary>
            /// <returns><see cref="int"/></returns>
            public int GetNetworkTick()
            {
                return NetworkTick;
            }

            /// <summary>
            /// Serializes this <see cref="NetworkTransformState"/>
            /// </summary>
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                // Used to calculate the LastSerializedSize value
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
                                if (!isWriting)
                                {
                                    NetworkDeltaPosition = new NetworkDeltaPosition(Vector3.zero, 0, math.bool3(HasPositionX, HasPositionY, HasPositionZ));
                                    NetworkDeltaPosition.NetworkSerialize(serializer);
                                }
                                else
                                {
                                    serializer.SerializeNetworkSerializable(ref NetworkDeltaPosition);
                                }
                            }
                        }
                        else
                        {
                            if (!isWriting)
                            {
                                NetworkDeltaPosition = new NetworkDeltaPosition(Vector3.zero, 0, math.bool3(HasPositionX, HasPositionY, HasPositionZ));
                                NetworkDeltaPosition.NetworkSerialize(serializer);
                            }
                            else
                            {
                                serializer.SerializeNetworkSerializable(ref NetworkDeltaPosition);
                            }
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
                        // Always use the full quaternion if teleporting
                        if (IsTeleportingNextFrame)
                        {
                            serializer.SerializeValue(ref Rotation);
                        }
                        else
                        {
                            // Use the quaternion compressor if enabled
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
                                        HalfVectorRotation.UpdateFrom(ref Rotation);
                                    }

                                    serializer.SerializeNetworkSerializable(ref HalfVectorRotation);

                                    if (!isWriting)
                                    {
                                        Rotation = HalfVectorRotation.ToQuaternion();
                                    }
                                }
                                else
                                {
                                    serializer.SerializeValue(ref Rotation);
                                }
                            }
                        }
                    }
                    else // Euler Rotation Synchronization
                    {
                        // Half float precision (full precision when teleporting)
                        if (UseHalfFloatPrecision && !IsTeleportingNextFrame)
                        {
                            if (HasRotAngleChange)
                            {
                                var halfPrecisionRotation = new HalfVector3(RotAngleX, RotAngleY, RotAngleZ, math.bool3(HasRotAngleX, HasRotAngleY, HasRotAngleZ));
                                serializer.SerializeValue(ref halfPrecisionRotation);
                                if (!isWriting)
                                {
                                    var eulerRotation = halfPrecisionRotation.ToVector3();
                                    if (HasRotAngleX)
                                    {
                                        RotAngleX = eulerRotation.x;
                                    }

                                    if (HasRotAngleY)
                                    {
                                        RotAngleY = eulerRotation.y;
                                    }

                                    if (HasRotAngleZ)
                                    {
                                        RotAngleZ = eulerRotation.z;
                                    }
                                }
                            }
                        }
                        else // Full precision Euler
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
                }

                // Synchronize Scale
                if (HasScaleChange)
                {
                    // Half precision scale synchronization
                    if (UseHalfFloatPrecision)
                    {
                        if (IsTeleportingNextFrame)
                        {
                            serializer.SerializeValue(ref Scale);
                        }
                        else
                        {
                            // For scale, when half precision is enabled we can still only send the axis with deltas
                            HalfVectorScale = new HalfVector3(Scale, math.bool3(HasScaleX, HasScaleY, HasScaleZ));
                            serializer.SerializeValue(ref HalfVectorScale);
                            if (!isWriting)
                            {
                                Scale = HalfVectorScale.ToVector3();
                                if (HasScaleX)
                                {
                                    ScaleX = Scale.x;
                                }

                                if (HasScaleY)
                                {
                                    ScaleY = Scale.y;
                                }

                                if (HasScaleZ)
                                {
                                    ScaleZ = Scale.x;
                                }
                            }
                        }
                    }
                    else // Full precision scale synchronization
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
                if (!isWriting)
                {
                    // Go ahead and mark the local state dirty
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
        /// When enabled (default), the x component of position will be synchronized by authority.
        /// </summary>
        /// <remarks>
        /// Changes to this on non-authoritative instances has no effect.
        /// </remarks>
        public bool SyncPositionX = true;

        /// <summary>
        /// When enabled (default), the y component of position will be synchronized by authority.
        /// </summary>
        /// <remarks>
        /// Changes to this on non-authoritative instances has no effect.
        /// </remarks>
        public bool SyncPositionY = true;

        /// <summary>
        /// When enabled (default), the z component of position will be synchronized by authority.
        /// </summary>
        /// <remarks>
        /// Changes to this on non-authoritative instances has no effect.
        /// </remarks>
        public bool SyncPositionZ = true;

        private bool SynchronizePosition
        {
            get
            {
                return SyncPositionX || SyncPositionY || SyncPositionZ;
            }
        }

        /// <summary>
        /// When enabled (default), the x component of rotation will be synchronized by authority.
        /// </summary>
        /// <remarks>
        /// When <see cref="UseQuaternionSynchronization"/> is enabled this does not apply.
        /// Changes to this on non-authoritative instances has no effect.
        /// </remarks>
        public bool SyncRotAngleX = true;

        /// <summary>
        /// When enabled (default), the y component of rotation will be synchronized by authority.
        /// </summary>
        /// <remarks>
        /// When <see cref="UseQuaternionSynchronization"/> is enabled this does not apply.
        /// Changes to this on non-authoritative instances has no effect.
        /// </remarks>
        public bool SyncRotAngleY = true;

        /// <summary>
        /// When enabled (default), the z component of rotation will be synchronized by authority.
        /// </summary>
        /// <remarks>
        /// When <see cref="UseQuaternionSynchronization"/> is enabled this does not apply.
        /// Changes to this on non-authoritative instances has no effect.
        /// </remarks>
        public bool SyncRotAngleZ = true;

        private bool SynchronizeRotation
        {
            get
            {
                return SyncRotAngleX || SyncRotAngleY || SyncRotAngleZ;
            }
        }

        /// <summary>
        /// When enabled (default), the x component of scale will be synchronized by authority.
        /// </summary>
        /// <remarks>
        /// Changes to this on non-authoritative instances has no effect.
        /// </remarks>
        public bool SyncScaleX = true;

        /// <summary>
        /// When enabled (default), the y component of scale will be synchronized by authority.
        /// </summary>
        /// <remarks>
        /// Changes to this on non-authoritative instances has no effect.
        /// </remarks>
        public bool SyncScaleY = true;

        /// <summary>
        /// When enabled (default), the z component of scale will be synchronized by authority.
        /// </summary>
        /// <remarks>
        /// Changes to this on non-authoritative instances has no effect.
        /// </remarks>
        public bool SyncScaleZ = true;

        private bool SynchronizeScale
        {
            get
            {
                return SyncScaleX || SyncScaleY || SyncScaleZ;
            }
        }

        /// <summary>
        /// The position threshold value that triggers a delta state update by the authoritative instance.
        /// </summary>
        /// <remarks>
        /// Note: setting this to zero will update position every network tick whether it changed or not.
        /// </remarks>
        public float PositionThreshold = PositionThresholdDefault;

        /// <summary>
        /// The rotation threshold value that triggers a delta state update by the authoritative instance.
        /// </summary>
        /// <remarks>
        /// Minimum Value: 0.00001
        /// Maximum Value: 360.0
        /// </remarks>
        [Range(0.00001f, 360.0f)]
        public float RotAngleThreshold = RotAngleThresholdDefault;

        /// <summary>
        /// The scale threshold value that triggers a delta state update by the authoritative instance.
        /// </summary>
        /// <remarks>
        /// Note: setting this to zero will update position every network tick whether it changed or not.
        /// </remarks>
        public float ScaleThreshold = ScaleThresholdDefault;

        /// <summary>
        /// Enable this on the authority side for quaternion synchronization
        /// </summary>
        /// <remarks>
        /// This is synchronized by authority. During runtime, this should only be changed by the
        /// authoritative side. Non-authoritative instances will be overridden by the next
        /// authoritative state update.
        /// </remarks>
        [Tooltip("When enabled, this will synchronize the full Quaternion (i.e. all Euler rotation axis are updated if one axis has a delta)")]
        public bool UseQuaternionSynchronization = false;

        /// <summary>
        /// Enabled this on the authority side for quaternion compression
        /// </summary>
        /// <remarks>
        /// This has a lower precision than half float precision. Recommended only for low precision
        /// scenarios. <see cref="UseHalfFloatPrecision"/> provides better precision at roughly half
        /// the cost of a full quaternion update.
        /// This is synchronized by authority. During runtime, this should only be changed by the
        /// authoritative side. Non-authoritative instances will be overridden by the next
        /// authoritative state update.
        /// </remarks>
        [Tooltip("When enabled, this uses a smallest three implementation that reduces full Quaternion updates down to the size of an unsigned integer (ignores half float precision settings).")]
        public bool UseQuaternionCompression = false;

        /// <summary>
        /// Enable this to use half float precision for position, rotation, and scale.
        /// When enabled, delta position synchronization is used.
        /// </summary>
        /// <remarks>
        /// This is synchronized by authority. During runtime, this should only be changed by the
        /// authoritative side. Non-authoritative instances will be overridden by the next
        /// authoritative state update.
        /// </remarks>
        [Tooltip("When enabled, this will use half float precision values for position (uses delta position updating), rotation (except when Quaternion compression is enabled), and scale.")]
        public bool UseHalfFloatPrecision = false;

        /// <summary>
        /// Sets whether the transform should be treated as local (true) or world (false) space.
        /// </summary>
        /// <remarks>
        /// This is synchronized by authority. During runtime, this should only be changed by the
        /// authoritative side. Non-authoritative instances will be overridden by the next
        /// authoritative state update.
        /// </remarks>
        [Tooltip("Sets whether this transform should sync in local space or in world space")]
        public bool InLocalSpace = false;

        /// <summary>
        /// When enabled (default) interpolation is applied.
        /// When disabled interpolation is disabled.
        /// </summary>
        /// <remarks>
        /// This is synchronized by authority and changes to interpolation during runtime forces a
        /// teleport/full update. During runtime, this should only be changed by the authoritative
        /// side. Non-authoritative instances will be overridden by the next authoritative state update.
        /// </remarks>
        public bool Interpolate = true;

        /// <summary>
        /// When true and interpolation is enabled, this will Slerp to the target position.
        /// </summary>
        /// <remarks>
        /// This is synchronized by authority and only applies to position interpolation.
        /// During runtime, this should only be changed by the authoritative side. Non-authoritative
        /// instances will be overridden by the next authoritative state update.
        /// </remarks>
        [Tooltip("When enabled the position interpolator will Slerp towards its current target position.")]
        public bool SlerpPosition = false;

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
        protected bool m_CachedIsServer; // Note: we no longer use this and are only keeping it until we decide to deprecate it

        /// <summary>
        /// Internally used by <see cref="NetworkTransform"/> to keep track of the <see cref="NetworkManager"/> instance assigned to this
        /// this <see cref="NetworkBehaviour"/> derived class instance.
        /// </summary>
        protected NetworkManager m_CachedNetworkManager; // Note: we no longer use this and are only keeping it until we decide to deprecate it

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

        /// <summary>
        /// Helper method that returns the space relative position of the transform.
        /// </summary>
        /// <remarks>
        /// If InLocalSpace is <see cref="true"/> then it returns the transform.localPosition
        /// If InLocalSpace is <see cref="false"/> then it returns the transform.position
        /// When invoked on the non-authority side:
        /// If <see cref="getCurrentState"/> is true then it will return the most
        /// current authority position from the most recent state update. This can be useful
        /// if interpolation is enabled and you need to determine the final target position.
        /// When invoked on the authority side:
        /// It will always return the space relative position.
        /// </remarks>
        /// <param name="getCurrentState">
        /// Authority always returns the space relative transform position (whether true or false).
        /// Non-authority:
        /// When false (default): returns the space relative transform position
        /// When true: returns the authority position from the most recent state update.
        /// </param>
        /// <returns><see cref="Vector3"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetSpaceRelativePosition(bool getCurrentState = false)
        {
            if (!getCurrentState || CanCommitToTransform)
            {
                return InLocalSpace ? transform.localPosition : transform.position;
            }
            else
            {
                return m_CurrentPosition;
            }
        }

        /// <summary>
        /// Helper method that returns the space relative rotation of the transform.
        /// </summary>
        /// <remarks>
        /// If InLocalSpace is <see cref="true"/> then it returns the transform.localRotation
        /// If InLocalSpace is <see cref="false"/> then it returns the transform.rotation
        /// When invoked on the non-authority side:
        /// If <see cref="getCurrentState"/> is true then it will return the most
        /// current authority rotation from the most recent state update. This can be useful
        /// if interpolation is enabled and you need to determine the final target rotation.
        /// When invoked on the authority side:
        /// It will always return the space relative rotation.
        /// </remarks>
        /// <param name="getCurrentState">
        /// Authority always returns the space relative transform rotation (whether true or false).
        /// Non-authority:
        /// When false (default): returns the space relative transform rotation
        /// When true: returns the authority rotation from the most recent state update.
        /// </param>
        /// <returns><see cref="Quaternion"/></returns>
        public Quaternion GetSpaceRelativeRotation(bool getCurrentState = false)
        {
            if (!getCurrentState || CanCommitToTransform)
            {
                return InLocalSpace ? transform.localRotation : transform.rotation;
            }
            else
            {
                return m_CurrentRotation;
            }
        }

        /// <summary>
        /// Helper method that returns the scale of the transform.
        /// </summary>
        /// <remarks>
        /// When invoked on the non-authority side:
        /// If <see cref="getCurrentState"/> is true then it will return the most
        /// current authority scale from the most recent state update. This can be useful
        /// if interpolation is enabled and you need to determine the final target scale.
        /// When invoked on the authority side:
        /// It will always return the space relative scale.
        /// </remarks>
        /// <param name="getCurrentState">
        /// Authority always returns the space relative transform scale (whether true or false).
        /// Non-authority:
        /// When false (default): returns the space relative transform scale
        /// When true: returns the authority scale from the most recent state update.
        /// </param>
        /// <returns><see cref="Vector3"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetScale(bool getCurrentState = false)
        {
            if (!getCurrentState || CanCommitToTransform)
            {
                return transform.localScale;
            }
            else
            {
                return m_CurrentScale;
            }
        }

        // Used by both authoritative and non-authoritative instances.
        // This represents the most recent local authoritative state.
        private NetworkTransformState m_LocalAuthoritativeNetworkState;

        private ClientRpcParams m_ClientRpcParams = new ClientRpcParams() { Send = new ClientRpcSendParams() };
        private List<ulong> m_ClientIds = new List<ulong>() { 0 };

        private BufferedLinearInterpolatorVector3 m_PositionInterpolator;
        private BufferedLinearInterpolatorVector3 m_ScaleInterpolator;
        private BufferedLinearInterpolatorQuaternion m_RotationInterpolator; // rotation is a single Quaternion since each Euler axis will affect the quaternion's final value

        // Non-Authoritative's current position, scale, and rotation that is used to assure the non-authoritative side cannot make adjustments to
        // the portions of the transform being synchronized.
        private Vector3 m_CurrentPosition;
        private Vector3 m_CurrentScale;
        private Quaternion m_CurrentRotation;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdatePositionInterpolator(Vector3 position, double time, bool resetInterpolator = false)
        {
            if (!CanCommitToTransform)
            {
                if (resetInterpolator)
                {
                    m_PositionInterpolator.ResetTo(position, time);
                }
                else
                {
                    m_PositionInterpolator.AddMeasurement(position, time);
                }
            }
        }

#if DEBUG_NETWORKTRANSFORM || UNITY_INCLUDE_TESTS
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
        protected NetworkDeltaPosition GetHalfPositionState()
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
        private NetworkDeltaPosition m_HalfPositionState = new NetworkDeltaPosition(Vector3.zero, 0);

        internal void UpdatePositionSlerp()
        {
            if (m_PositionInterpolator != null)
            {
                m_PositionInterpolator.IsSlerp = SlerpPosition;
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
        /// <remarks>
        /// If a derived class overrides this, then make sure to invoke this base method!
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        /// <param name="targetClientId">the clientId being synchronized (both reading and writing)</param>
        protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
        {
            var targetClientId = m_TargetIdBeingSynchronized;
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
            if (!IsSpawned)
            {
                NetworkLog.LogError($"Cannot commit transform when not spawned!");
                return;
            }

            // Only the server or the owner is allowed to commit a transform
            if (!IsServer && !IsOwner)
            {
                var errorMessage = gameObject != NetworkObject.gameObject ?
                    $"Non-authority instance of {NetworkObject.gameObject.name} is trying to commit a transform on {gameObject.name}!" :
                    $"Non-authority instance of {NetworkObject.gameObject.name} is trying to commit a transform!";
                NetworkLog.LogError(errorMessage);
                return;
            }

            // If we are authority, update the authoritative state
            if (CanCommitToTransform)
            {
                OnUpdateAuthoritativeState(ref transformToCommit);
            }
            else // Non-Authority
            {
                var position = InLocalSpace ? transformToCommit.localPosition : transformToCommit.position;
                var rotation = InLocalSpace ? transformToCommit.localRotation : transformToCommit.rotation;
                // We are an owner requesting to update our state
                if (!IsServer)
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
        /// Invoked just prior to being pushed to non-authority instances.
        /// </summary>
        /// <remarks>
        /// This is useful to know the exact position, rotation, or scale values sent
        /// to non-authoritative instances. This is only invoked on the authoritative
        /// instance.
        /// </remarks>
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
            // Only the server or the owner is allowed to commit a transform
            if (!IsServer && !IsOwner)
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

                m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame = false;
            }
        }

        /// <summary>
        /// Initializes the interpolators with the current transform values
        /// </summary>
        private void ResetInterpolatedStateToCurrentAuthoritativeState()
        {
            var serverTime = NetworkManager.ServerTime.Time;

            UpdatePositionInterpolator(GetSpaceRelativePosition(), serverTime, true);
            UpdatePositionSlerp();

            m_ScaleInterpolator.ResetTo(transform.localScale, serverTime);
            m_RotationInterpolator.ResetTo(GetSpaceRelativeRotation(), serverTime);
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
            networkState.QuaternionCompression = UseQuaternionCompression;
            m_HalfPositionState = new NetworkDeltaPosition(Vector3.zero, 0, math.bool3(SyncPositionX, SyncPositionY, SyncPositionZ));

            return ApplyTransformToNetworkStateWithInfo(ref networkState, ref transformToUse);
        }

        /// <summary>
        /// Applies the transform to the <see cref="NetworkTransformState"/> specified.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ApplyTransformToNetworkStateWithInfo(ref NetworkTransformState networkState, ref Transform transformToUse, bool isSynchronization = false, ulong targetClientId = 0)
        {
            var isTeleportingAndNotSynchronizing = networkState.IsTeleportingNextFrame && !isSynchronization;
            var isDirty = false;
            var isPositionDirty = isTeleportingAndNotSynchronizing ? networkState.HasPositionChange : false;
            var isRotationDirty = isTeleportingAndNotSynchronizing ? networkState.HasRotAngleChange : false;
            var isScaleDirty = isTeleportingAndNotSynchronizing ? networkState.HasScaleChange : false;

            var position = InLocalSpace ? transformToUse.localPosition : transformToUse.position;
            var rotAngles = InLocalSpace ? transformToUse.localEulerAngles : transformToUse.eulerAngles;
            var scale = transformToUse.localScale;

            networkState.IsSynchronizing = isSynchronization;

            if (InLocalSpace != networkState.InLocalSpace)
            {
                networkState.InLocalSpace = InLocalSpace;
                isDirty = true;
                networkState.IsTeleportingNextFrame = true;
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
            else if (SynchronizePosition)
            {
                // If we are teleporting then we can skip the delta threshold check
                isPositionDirty = networkState.IsTeleportingNextFrame;

                // For NetworkDeltaPosition, if any axial value is dirty then we always send a full update
                if (!isPositionDirty)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (Math.Abs(position[i] - m_HalfPositionState.PreviousPosition[i]) >= PositionThreshold)
                        {
                            isPositionDirty = i == 0 ? SyncPositionX : i == 1 ? SyncPositionY : SyncPositionZ;
                            if (!isPositionDirty)
                            {
                                continue;
                            }
                            break;
                        }
                    }
                }

                // If the position is dirty or we are teleporting (which includes synchronization)
                // then determine what parts of the NetworkDeltaPosition should be updated
                if (isPositionDirty)
                {
                    // If we are not synchronizing the transform state for the first time
                    if (!isSynchronization)
                    {
                        // With global teleporting (broadcast to all non-authority instances)
                        // we re-initialize authority's NetworkDeltaPosition and synchronize all
                        // non-authority instances with the new full precision position
                        if (networkState.IsTeleportingNextFrame)
                        {
                            m_HalfPositionState = new NetworkDeltaPosition(position, networkState.NetworkTick, math.bool3(SyncPositionX, SyncPositionY, SyncPositionZ));
                            networkState.CurrentPosition = position;
                        }
                        else // Otherwise, just synchronize the delta position value
                        {
                            m_HalfPositionState.HalfVector3.AxisToSynchronize = math.bool3(SyncPositionX, SyncPositionY, SyncPositionZ);
                            m_HalfPositionState.UpdateFrom(ref position, networkState.NetworkTick);
                        }

                        networkState.NetworkDeltaPosition = m_HalfPositionState;
                    }
                    else // If synchronizing is set, then use the current full position value on the server side
                    {
                        if (ShouldSynchronizeHalfFloat(targetClientId))
                        {
                            // If we have a NetworkDeltaPosition that has a state applied, then we want to determine
                            // what needs to be synchronized. For owner authoritative mode, the server side
                            // will have no valid state yet.
                            if (m_HalfPositionState.NetworkTick > 0)
                            {
                                // Always synchronize the base position and the ushort values of the
                                // current m_HalfPositionState
                                networkState.CurrentPosition = m_HalfPositionState.CurrentBasePosition;
                                networkState.NetworkDeltaPosition = m_HalfPositionState;
                                // If the server is the owner, in both server and owner authoritative modes,
                                // or we are running in server authoritative mode, then we use the
                                // HalfDeltaConvertedBack value as the delta position
                                if (NetworkObject.IsOwnedByServer || IsServerAuthoritative())
                                {
                                    networkState.DeltaPosition = m_HalfPositionState.HalfDeltaConvertedBack;
                                }
                                else
                                {
                                    // Otherwise, we are in owner authoritative mode and the server's NetworkDeltaPosition
                                    // state is "non-authoritative" relative so we use the DeltaPosition.
                                    networkState.DeltaPosition = m_HalfPositionState.DeltaPosition;
                                }
                            }
                            else // Reset everything and just send the current position
                            {
                                networkState.NetworkDeltaPosition = new NetworkDeltaPosition(Vector3.zero, 0, math.bool3(SyncPositionX, SyncPositionY, SyncPositionZ));
                                networkState.DeltaPosition = Vector3.zero;
                                networkState.CurrentPosition = position;
                            }
                        }
                        else
                        {
                            networkState.NetworkDeltaPosition = new NetworkDeltaPosition(Vector3.zero, 0, math.bool3(SyncPositionX, SyncPositionY, SyncPositionZ));
                            networkState.CurrentPosition = position;
                        }
                        // Add log entry for this update relative to the client being synchronized
                        AddLogEntry(ref networkState, targetClientId, true);
                    }
                    networkState.HasPositionX = SyncPositionX;
                    networkState.HasPositionY = SyncPositionY;
                    networkState.HasPositionZ = SyncPositionZ;
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
            else if (SynchronizeRotation)
            {
                // If we are teleporting then we can skip the delta threshold check
                isRotationDirty = networkState.IsTeleportingNextFrame;
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
                if (!UseHalfFloatPrecision)
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
                else if (SynchronizeScale)
                {
                    var previousScale = networkState.Scale;
                    for (int i = 0; i < 3; i++)
                    {
                        if (Mathf.Abs(Mathf.DeltaAngle(previousScale[i], scale[i])) >= ScaleThreshold || networkState.IsTeleportingNextFrame)
                        {
                            isScaleDirty = true;
                            networkState.Scale[i] = scale[i];
                            networkState.SetHasScale(i, i == 0 ? SyncScaleX : i == 1 ? SyncScaleY : SyncScaleZ);
                        }
                    }
                }
            }
            else // If we are synchronizing then we need to determine which scale to use
            if (SynchronizeScale)
            {
                // This all has to do with complex nested hierarchies and how it impacts scale
                // when set for the first time.
                var hasParentNetworkObject = false;

                // If the NetworkObject belonging to this NetworkTransform instance has a parent
                // (i.e. this handles nested NetworkTransforms under a parent at some layer above)
                if (NetworkObject.transform.parent != null)
                {
                    var parentNetworkObject = NetworkObject.transform.parent.GetComponent<NetworkObject>();

                    // In-scene placed NetworkObjects parented under a GameObject with no
                    // NetworkObject preserve their lossyScale when synchronizing.
                    if (parentNetworkObject == null && NetworkObject.IsSceneObject != false)
                    {
                        hasParentNetworkObject = true;
                    }
                    else
                    {
                        // Or if the relative NetworkObject has a parent NetworkObject
                        hasParentNetworkObject = parentNetworkObject != null;
                    }
                }

                // If world position stays is set and the relative NetworkObject is parented under a NetworkObject
                // then we want to use the lossy scale for the initial synchronization.
                var useLossy = NetworkObject.WorldPositionStays() && hasParentNetworkObject;
                var scaleToUse = useLossy ? transform.lossyScale : transform.localScale;

                if (!UseHalfFloatPrecision)
                {
                    networkState.ScaleX = scaleToUse.x;
                    networkState.ScaleY = scaleToUse.y;
                    networkState.ScaleZ = scaleToUse.z;
                }
                else
                {
                    networkState.Scale = scaleToUse;
                }
                networkState.HasScaleX = true;
                networkState.HasScaleY = true;
                networkState.HasScaleZ = true;
                isScaleDirty = true;
            }
            isDirty |= isPositionDirty || isRotationDirty || isScaleDirty;

            if (isDirty)
            {
                // Some integration/unit tests disable the NetworkTransform and there is no
                // NetworkManager
                if (enabled)
                {
                    networkState.NetworkTick = NetworkManager.ServerTime.Tick;
                }
            }

            // Mark the state dirty for the next network tick update to clear out the bitset values
            networkState.IsDirty |= isDirty;
            return isDirty;
        }

        /// <summary>
        /// Applies the authoritative state to the transform
        /// </summary>
        private void ApplyAuthoritativeState()
        {
            var networkState = m_LocalAuthoritativeNetworkState;
            // The m_CurrentPosition, m_CurrentRotation, and m_CurrentScale values are continually updated
            // at the end of this method and assure that when not interpolating the non-authoritative side
            // cannot make adjustments to any portions the transform not being synchronized.
            var adjustedPosition = m_CurrentPosition;
            var adjustedRotation = m_CurrentRotation;
            var adjustedRotAngles = adjustedRotation.eulerAngles;
            var adjustedScale = m_CurrentScale;

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
                if (SynchronizePosition)
                {
                    var interpolatedPosition = m_PositionInterpolator.GetInterpolatedValue();
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
                }

                if (SynchronizeScale)
                {
                    if (UseHalfFloatPrecision)
                    {
                        adjustedScale = m_ScaleInterpolator.GetInterpolatedValue();
                    }
                    else
                    {
                        var interpolatedScale = m_ScaleInterpolator.GetInterpolatedValue();
                        if (SyncScaleX) { adjustedScale.x = interpolatedScale.x; }
                        if (SyncScaleY) { adjustedScale.y = interpolatedScale.y; }
                        if (SyncScaleZ) { adjustedScale.z = interpolatedScale.z; }
                    }
                }

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
                // Non-Interpolated Position and Scale
                if (UseHalfFloatPrecision)
                {
                    if (networkState.HasPositionChange && SynchronizePosition)
                    {
                        adjustedPosition = networkState.CurrentPosition;
                    }

                    if (networkState.HasScaleChange && SynchronizeScale)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (m_LocalAuthoritativeNetworkState.HasScale(i))
                            {
                                adjustedScale[i] = m_LocalAuthoritativeNetworkState.Scale[i];
                            }
                        }
                    }
                }
                else
                {
                    if (networkState.HasPositionX) { adjustedPosition.x = networkState.PositionX; }
                    if (networkState.HasPositionY) { adjustedPosition.y = networkState.PositionY; }
                    if (networkState.HasPositionZ) { adjustedPosition.z = networkState.PositionZ; }
                    if (networkState.HasScaleX) { adjustedScale.x = networkState.ScaleX; }
                    if (networkState.HasScaleY) { adjustedScale.y = networkState.ScaleY; }
                    if (networkState.HasScaleZ) { adjustedScale.z = networkState.ScaleZ; }
                }

                // Non-interpolated rotation
                if (SynchronizeRotation)
                {
                    if (networkState.QuaternionSync && networkState.HasRotAngleChange)
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
            }

            // Apply the position if we are synchronizing position
            if (SynchronizePosition)
            {
                // Update our current position if it changed or we are interpolating
                if (networkState.HasPositionChange || Interpolate)
                {
                    m_CurrentPosition = adjustedPosition;
                }
                if (InLocalSpace)
                {
                    transform.localPosition = m_CurrentPosition;
                }
                else
                {
                    transform.position = m_CurrentPosition;
                }
            }

            // Apply the rotation if we are synchronizing rotation
            if (SynchronizeRotation)
            {
                // Update our current rotation if it changed or we are interpolating
                if (networkState.HasRotAngleChange || Interpolate)
                {
                    m_CurrentRotation = adjustedRotation;
                }
                if (InLocalSpace)
                {
                    transform.localRotation = m_CurrentRotation;
                }
                else
                {
                    transform.rotation = m_CurrentRotation;
                }
            }

            // Apply the scale if we are synchronizing scale
            if (SynchronizeScale)
            {
                // Update our current scale if it changed or we are interpolating
                if (networkState.HasScaleChange || Interpolate)
                {
                    m_CurrentScale = adjustedScale;
                }
                transform.localScale = m_CurrentScale;
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

            // Clear all interpolators
            m_ScaleInterpolator.Clear();
            m_PositionInterpolator.Clear();
            m_RotationInterpolator.Clear();

            if (newState.HasPositionChange)
            {
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
                    // With delta position teleport updates or synchronization, we create a new instance and provide the current network tick.
                    m_HalfPositionState = new NetworkDeltaPosition(newState.CurrentPosition, newState.NetworkTick, math.bool3(SyncPositionX, SyncPositionY, SyncPositionZ));

                    // When first synchronizing we determine if we need to apply the current delta position
                    // offset or not. This is specific to owner authoritative mode on the owner side only
                    if (isSynchronization)
                    {
                        if (ShouldSynchronizeHalfFloat(NetworkManager.LocalClientId))
                        {
                            m_HalfPositionState.HalfVector3.Axis = newState.NetworkDeltaPosition.HalfVector3.Axis;
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
                        // If we are just teleporting, then we already created a new NetworkDeltaPosition value.
                        // set the current position to the state's current position
                        currentPosition = newState.CurrentPosition;
                    }

                    if (Interpolate)
                    {
                        UpdatePositionInterpolator(currentPosition, sentTime, true);
                    }

                }

                m_CurrentPosition = currentPosition;

                // Apply the position
                if (newState.InLocalSpace)
                {
                    transform.localPosition = currentPosition;
                }
                else
                {
                    transform.position = currentPosition;
                }
            }

            if (newState.HasScaleChange)
            {
                if (UseHalfFloatPrecision)
                {
                    currentScale = newState.Scale;
                    m_CurrentScale = currentScale;
                }
                else
                {
                    // Adjust based on which axis changed
                    if (newState.HasScaleX)
                    {
                        currentScale.x = newState.ScaleX;
                    }

                    if (newState.HasScaleY)
                    {
                        currentScale.y = newState.ScaleY;
                    }

                    if (newState.HasScaleZ)
                    {
                        currentScale.z = newState.ScaleZ;
                    }

                }

                m_CurrentScale = currentScale;
                m_ScaleInterpolator.ResetTo(currentScale, sentTime);

                // Apply the adjusted scale
                transform.localScale = currentScale;
            }

            if (newState.HasRotAngleChange)
            {
                if (newState.QuaternionSync)
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

                m_CurrentRotation = currentRotation;
                m_RotationInterpolator.ResetTo(currentRotation, sentTime);

                if (InLocalSpace)
                {
                    transform.localRotation = currentRotation;
                }
                else
                {
                    transform.rotation = currentRotation;
                }
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

            // Only if using half float precision and our position had changed last update then
            if (UseHalfFloatPrecision && m_LocalAuthoritativeNetworkState.HasPositionChange)
            {
                // assure our local NetworkDeltaPosition state is updated
                m_HalfPositionState.HalfVector3.Axis = m_LocalAuthoritativeNetworkState.NetworkDeltaPosition.HalfVector3.Axis;
                // and update our current position
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

            if (m_LocalAuthoritativeNetworkState.HasScaleChange)
            {
                var currentScale = transform.localScale;
                if (UseHalfFloatPrecision)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (m_LocalAuthoritativeNetworkState.HasScale(i))
                        {
                            currentScale[i] = m_LocalAuthoritativeNetworkState.Scale[i];
                        }
                    }
                }
                else
                {
                    if (m_LocalAuthoritativeNetworkState.HasScaleX)
                    {
                        currentScale.x = m_LocalAuthoritativeNetworkState.ScaleX;
                    }

                    if (m_LocalAuthoritativeNetworkState.HasScaleY)
                    {
                        currentScale.y = m_LocalAuthoritativeNetworkState.ScaleY;
                    }

                    if (m_LocalAuthoritativeNetworkState.HasScaleZ)
                    {
                        currentScale.z = m_LocalAuthoritativeNetworkState.ScaleZ;
                    }
                }
                m_ScaleInterpolator.AddMeasurement(currentScale, sentTime);
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
        /// This value roughly translates to the maximum value of 't' in <see cref="Vector3.Lerp(Vector3, Vector3, float)"/> and
        /// <see cref="Quaternion.Lerp(Quaternion, Quaternion, float)"/> for all transform elements being monitored by
        /// <see cref="NetworkTransform"/> (i.e. Position, Scale, and Rotation)
        /// </summary>
        /// <param name="maxInterpolationBound">Maximum time boundary that can be used in a frame when interpolating between two values</param>
        public void SetMaxInterpolationBound(float maxInterpolationBound)
        {
            m_RotationInterpolator.MaxInterpolationBound = maxInterpolationBound;
            m_PositionInterpolator.MaxInterpolationBound = maxInterpolationBound;
            m_ScaleInterpolator.MaxInterpolationBound = maxInterpolationBound;
        }

        /// <summary>
        /// Create interpolators when first instantiated to avoid memory allocations if the
        /// associated NetworkObject persists (i.e. despawned but not destroyed or pools)
        /// </summary>
        protected virtual void Awake()
        {
            // Rotation is a single Quaternion since each Euler axis will affect the quaternion's final value
            m_RotationInterpolator = new BufferedLinearInterpolatorQuaternion();
            m_PositionInterpolator = new BufferedLinearInterpolatorVector3();
            m_ScaleInterpolator = new BufferedLinearInterpolatorVector3();
        }

        /// <summary>
        /// Checks for changes in the axis to synchronize. If one or more did change it
        /// then determines if the axis were enabled and if the delta between the last known
        /// delta position and the current position for the axis exceeds the adjustment range
        /// before it is collapsed into the base position.
        /// If it does exceed the adjustment range, then we have to teleport the object so
        /// a full position synchronization takes place and the NetworkDeltaPosition is
        /// reset with the updated base position that it then will generating a new delta position from.
        /// </summary>
        /// <remarks>
        /// This only happens if a user disables an axis, continues to update the disabled axis,
        /// and then later enables the axis. (which will not be a recommended best practice)
        /// </remarks>
        private void AxisChangedDeltaPositionCheck()
        {
            if (UseHalfFloatPrecision && SynchronizePosition)
            {
                var synAxis = m_HalfPositionState.HalfVector3.AxisToSynchronize;
                if (SyncPositionX != synAxis.x || SyncPositionY != synAxis.y || SyncPositionZ != synAxis.z)
                {
                    var positionState = m_HalfPositionState.GetFullPosition();
                    var relativePosition = GetSpaceRelativePosition();
                    bool needsToTeleport = false;
                    // Only if the synchronization of an axis is turned on do we need to
                    // check if a teleport is required due to the delta from the last known
                    // to the currently known axis value exceeds MaxDeltaBeforeAdjustment.
                    if (SyncPositionX && SyncPositionX != synAxis.x)
                    {
                        needsToTeleport = Mathf.Abs(relativePosition.x - positionState.x) >= NetworkDeltaPosition.MaxDeltaBeforeAdjustment;
                    }
                    if (SyncPositionY && SyncPositionY != synAxis.y)
                    {
                        needsToTeleport = Mathf.Abs(relativePosition.y - positionState.y) >= NetworkDeltaPosition.MaxDeltaBeforeAdjustment;
                    }
                    if (SyncPositionZ && SyncPositionZ != synAxis.z)
                    {
                        needsToTeleport = Mathf.Abs(relativePosition.z - positionState.z) >= NetworkDeltaPosition.MaxDeltaBeforeAdjustment;
                    }
                    // If needed, force a teleport as the delta is outside of the valid delta boundary
                    m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame = needsToTeleport;
                }
            }
        }

        /// <summary>
        /// Called by authority to check for deltas and update non-authoritative instances
        /// if any are found.
        /// </summary>
        internal void OnUpdateAuthoritativeState(ref Transform transformSource)
        {
            // If our replicated state is not dirty and our local authority state is dirty, clear it.
            if (!ReplicatedNetworkState.IsDirty() && m_LocalAuthoritativeNetworkState.IsDirty && !m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame)
            {
                // Now clear our bitset and prepare for next network tick state update
                m_LocalAuthoritativeNetworkState.ClearBitSetForNextTick();
            }

            AxisChangedDeltaPositionCheck();

            TryCommitTransform(ref transformSource);
        }

        /// <summary>
        /// Authority subscribes to network tick events and will invoke
        /// <see cref="OnUpdateAuthoritativeState(ref Transform)"/> each network tick.
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
            // NOTE: Legacy and no longer used (candidates for deprecation)
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
        /// Invoked when first spawned and when ownership changes.
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
                    m_HalfPositionState = new NetworkDeltaPosition(currentPosition, NetworkManager.NetworkTickSystem.ServerTime.Tick, math.bool3(SyncPositionX, SyncPositionY, SyncPositionZ));
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
                m_CurrentPosition = GetSpaceRelativePosition();
                m_CurrentScale = transform.localScale;
                m_CurrentRotation = GetSpaceRelativeRotation();

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
                NetworkLog.LogError($"Cannot commit transform when not spawned!");
                return;
            }

            // Only the server or the owner is allowed to commit a transform
            if (!IsServer && !IsOwner)
            {
                var errorMessage = gameObject != NetworkObject.gameObject ?
                    $"Non-authority instance of {NetworkObject.gameObject.name} is trying to commit a transform on {gameObject.name}!" :
                    $"Non-authority instance of {NetworkObject.gameObject.name} is trying to commit a transform!";
                NetworkLog.LogError(errorMessage);
                return;
            }

            Vector3 pos = posIn == null ? GetSpaceRelativePosition() : posIn.Value;
            Quaternion rot = rotIn == null ? GetSpaceRelativeRotation() : rotIn.Value;
            Vector3 scale = scaleIn == null ? transform.localScale : scaleIn.Value;

            if (!CanCommitToTransform)
            {
                // Preserving the ability for owner authoritative mode to accept state changes from server
                if (IsServer)
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
        /// - Non-authority always invokes this base class method.
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
                var cachedDeltaTime = NetworkManager.RealTimeProvider.DeltaTime;
                var cachedServerTime = serverTime.Time;
                // TODO: Investigate Further
                // With owner authoritative mode, non-authority clients can lag behind
                // by more than 1 tick period of time. The current "solution" for now
                // is to make their cachedRenderTime run 2 ticks behind.
                var ticksAgo = !IsServerAuthoritative() && !IsServer ? 2 : 1;
                var cachedRenderTime = serverTime.TimeTicksAgo(ticksAgo).Time;

                // Now only update the interpolators for the portions of the transform being synchronized
                if (SynchronizePosition)
                {
                    m_PositionInterpolator.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);
                }

                if (SynchronizeRotation)
                {
                    // When using half precision Lerp towards the target rotation.
                    // When using full precision Slerp towards the target rotation.
                    /// <see cref="BufferedLinearInterpolatorQuaternion.IsSlerp"/>
                    m_RotationInterpolator.IsSlerp = !UseHalfFloatPrecision;
                    m_RotationInterpolator.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);
                }

                if (SynchronizeScale)
                {
                    m_ScaleInterpolator.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);
                }
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
        /// Method to determine if this <see cref="NetworkTransform"/> instance is owner or server authoritative.
        /// </summary>
        /// <remarks>
        /// Used by <see cref="NetworkRigidbody"/> to determines if this is server or owner authoritative.
        /// </remarks>
        /// <returns><see cref="true"/> or <see cref="false"/></returns>
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
