using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Half float precision <see cref="Vector3"/> used to synchromnize delta position.
    /// </summary>
    public struct HalfVector3DeltaPosition : INetworkSerializable
    {
        /// <summary>
        /// The half float precision value of the x-axis as a <see cref="ushort"/>.
        /// </summary>
        public ushort X => Axis.X;
        /// <summary>
        /// The half float precision value of the y-axis as a <see cref="ushort"/>.
        /// </summary>
        public ushort Y => Axis.Y;
        /// <summary>
        /// The half float precision value of the z-axis as a <see cref="ushort"/>.
        /// </summary>
        public ushort Z => Axis.Z;

        /// <summary>
        /// Used to store the half float precision value as a <see cref="ushort"/>.
        /// </summary>
        public Vector3T<ushort> Axis;

        internal Vector3 CurrentBasePosition;
        internal Vector3 PrecisionLossDelta;
        internal Vector3 HalfDeltaConvertedBack;
        internal Vector3 PreviousPosition;
        internal Vector3 DeltaPosition;
        internal int NetworkTick;

        internal const float MaxDeltaBeforeAdjustment = 64f;

        internal Vector3AxisToSynchronize AxisToSynchronize;

        /// <summary>
        /// The serialization implementation of <see cref="INetworkSerializable"/>
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            for (int i = 0; i < Axis.Length; i++)
            {
                if (AxisToSynchronize.SyncAxis[i])
                {
                    var axisValue = Axis[i];
                    serializer.SerializeValue(ref axisValue);
                    if (serializer.IsReader)
                    {
                        Axis[i] = axisValue;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the full precision value of Vector3 position while also potentially updating the current base position.
        /// </summary>
        /// <param name="networkTick">Use the current network tick value.</param>
        /// <returns>The full position as a <see cref="Vector3"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 ToVector3(int networkTick)
        {
            // When synchronizing, it is possible to have a state update arrive
            // for the same synchronization network tick.  Under this scenario,
            // we only want to return the existing CurrentBasePosition + DeltaPosition
            // values and not process the X, Y, or Z values.
            // (See the constructors below)
            if (networkTick == NetworkTick)
            {
                return CurrentBasePosition + DeltaPosition;
            }
            for (int i = 0; i < Axis.Length; i++)
            {
                if (AxisToSynchronize.SyncAxis[i])
                {
                    DeltaPosition[i] = Mathf.HalfToFloat(Axis[i]);
                    // If we exceed or are equal to the maximum delta value then we need to
                    // apply the delta to the CurrentBasePosition value and reset the delta
                    // position for the axis.
                    if (Mathf.Abs(DeltaPosition[i]) >= MaxDeltaBeforeAdjustment)
                    {
                        CurrentBasePosition[i] += DeltaPosition[i];
                        DeltaPosition[i] = 0.0f;
                        Axis[i] = 0;
                    }
                }
            }
            return CurrentBasePosition + DeltaPosition;
        }

        /// <summary>
        /// Updates the axis to be synchronized.
        /// </summary>
        /// <param name="vector3AxisToSynchronize">Updated <see cref="Vector3AxisToSynchronize"/> values.</param>
        public void UpdateAxisToSynchronize(Vector3AxisToSynchronize vector3AxisToSynchronize)
        {
            AxisToSynchronize = vector3AxisToSynchronize;
        }

        /// <summary>
        /// Returns the current base position (excluding the delta position offset).
        /// </summary>
        /// <returns>The current base position as a <see cref="Vector3"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetCurrentBasePosition()
        {
            return CurrentBasePosition;
        }

        /// <summary>
        /// Returns the full position which includes the delta offset position.
        /// </summary>
        /// <returns>The full position as a <see cref="Vector3"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetFullPosition()
        {
            return CurrentBasePosition + DeltaPosition;
        }

        /// <summary>
        /// The half float vector3 version of the current delta position.
        /// </summary>
        /// <remarks>
        /// Only applies to the authoritative side for <see cref="NetworkTransform"/> instances.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetConvertedDelta()
        {
            return HalfDeltaConvertedBack;
        }

        /// <summary>
        /// The full precision current delta position.
        /// </summary>
        /// <remarks>
        /// Authoritative: Will have no precision loss
        /// Non-Authoritative: Has the current network tick's loss of precision.
        /// Precision loss adjustments are one network tick behind on the
        /// non-authoritative side.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetDeltaPosition()
        {
            return DeltaPosition;
        }

        /// <summary>
        /// Updates the position delta based off of the current base position.
        /// </summary>
        /// <param name="vector3">The full precision <see cref="Vector3"/> value to (converted to half floats) used to determine the delta offset positon.</param>
        /// <param name="networkTick">Set the current network tick value when updating.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateFrom(ref Vector3 vector3, int networkTick)
        {
            NetworkTick = networkTick;
            DeltaPosition = (vector3 + PrecisionLossDelta) - CurrentBasePosition;

            for (int i = 0; i < Axis.Length; i++)
            {
                if (AxisToSynchronize.SyncAxis[i])
                {
                    Axis[i] = Mathf.FloatToHalf(DeltaPosition[i]);
                    HalfDeltaConvertedBack[i] = Mathf.HalfToFloat(Axis[i]);
                    PrecisionLossDelta[i] = DeltaPosition[i] - HalfDeltaConvertedBack[i];
                    if (Mathf.Abs(HalfDeltaConvertedBack[i]) >= MaxDeltaBeforeAdjustment)
                    {
                        CurrentBasePosition[i] += HalfDeltaConvertedBack[i];
                        HalfDeltaConvertedBack[i] = 0.0f;
                        DeltaPosition[i] = 0.0f;
                    }
                }
            }

            for (int i = 0; i < Axis.Length; i++)
            {
                if (AxisToSynchronize.SyncAxis[i])
                {
                    PreviousPosition[i] = vector3[i];
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vector3">The initial axial values (converted to half floats) when instantiated.</param>
        /// <param name="networkTick">Set the network tick value to the current network tick when instantiating.</param>
        /// <param name="vector3AxisToSynchronize">The axis to be synchronized.</param>
        public HalfVector3DeltaPosition(Vector3 vector3, int networkTick, Vector3AxisToSynchronize vector3AxisToSynchronize)
        {
            AxisToSynchronize = vector3AxisToSynchronize;
            Axis = default;
            NetworkTick = networkTick;
            CurrentBasePosition = vector3;
            PreviousPosition = vector3;
            PrecisionLossDelta = Vector3.zero;
            DeltaPosition = Vector3.zero;
            HalfDeltaConvertedBack = Vector3.zero;
            UpdateFrom(ref vector3, networkTick);
        }

        /// <summary>
        /// Constructor that defaults to all axis being synchronized.
        /// </summary>
        /// <param name="vector3">The initial axial values (converted to half floats) when instantiated.</param>
        /// <param name="networkTick">Set the network tick value to the current network tick when instantiating.</param>
        public HalfVector3DeltaPosition(Vector3 vector3, int networkTick) : this(vector3, networkTick, Vector3AxisToSynchronize.AllAxis)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="x">The initial x axis (converted to half float) value when instantiated.</param>
        /// <param name="y">The initial y axis (converted to half float) value when instantiated.</param>
        /// <param name="z">The initial z axis (converted to half float) value when instantiated.</param>
        /// <param name="networkTick">Set the network tick value to the current network tick when instantiating.</param>
        /// <param name="vector3AxisToSynchronize">The axis to be synchronized.</param>
        public HalfVector3DeltaPosition(float x, float y, float z, int networkTick, Vector3AxisToSynchronize vector3AxisToSynchronize) :
            this(new Vector3(x, y, z), networkTick, vector3AxisToSynchronize)
        {
        }

        /// <summary>
        /// Constructor that defaults to all axis being synchronized
        /// </summary>
        /// <param name="x">The initial x axis (converted to half float) value when instantiated.</param>
        /// <param name="y">The initial y axis (converted to half float) value when instantiated.</param>
        /// <param name="z">The initial z axis (converted to half float) value when instantiated.</param>
        /// <param name="networkTick">Set the network tick value to the current network tick when instantiating.</param>
        public HalfVector3DeltaPosition(float x, float y, float z, int networkTick) :
            this(new Vector3(x, y, z), networkTick, Vector3AxisToSynchronize.AllAxis)
        {
        }
    }
}
