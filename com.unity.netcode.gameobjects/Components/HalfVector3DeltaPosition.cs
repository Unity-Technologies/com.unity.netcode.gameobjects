using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Half float precision <see cref="Vector3"/> that is used for delta position
    /// synchronization.
    /// </summary>
    public struct HalfVector3DeltaPosition : INetworkSerializable
    {
        /// <summary>
        /// The <see cref="ushort"/> half float delta position X value
        /// </summary>
        public ushort X => Axis[0];
        /// <summary>
        /// The <see cref="ushort"/> half float delta position Y value
        /// </summary>
        public ushort Y => Axis[1];
        /// <summary>
        /// The <see cref="ushort"/> half float delta position Z value
        /// </summary>
        public ushort Z => Axis[2];

        public ushort[] Axis;

        internal Vector3 CurrentBasePosition;
        internal Vector3 PrecisionLossDelta;
        internal Vector3 HalfDeltaConvertedBack;
        internal Vector3 PreviousPosition;
        internal Vector3 DeltaPosition;
        internal int NetworkTick;

        private const float k_MaxDeltaBeforeAdjustment = 64f;

        private HalfVector3AxisToSynchronize m_HalfVector3AxisToSynchronize;

        /// <summary>
        /// The serialization implementation of <see cref="INetworkSerializable"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            for (int i = 0; i < 3; i++)
            {
                if (m_HalfVector3AxisToSynchronize.SyncAxis[i])
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
        /// Gets the full Vector3 position.
        /// </summary>
        /// <param name="tick">the network tick state applied to this HalfVector3DeltaPosition</param>
        /// <returns>the <see cref="Vector3"/> full position value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 ToVector3(int tick)
        {
            // When synchronizing, it is possible to have a state update arrive
            // for the same synchronization network tick.  Under this scenario,
            // we only want to return the existing CurrentBasePosition + DeltaPosition
            // values and not process the X, Y, or Z values.
            // (See the constructors below)
            if (tick == NetworkTick)
            {
                return CurrentBasePosition + DeltaPosition;
            }
            for (int i = 0; i < 3; i++)
            {
                if (m_HalfVector3AxisToSynchronize.SyncAxis[i])
                {
                    DeltaPosition[i] = Mathf.HalfToFloat(Axis[i]);
                    // If we exceed or are equal to the maximum delta value then we need to
                    // apply the delta to the CurrentBasePosition value and reset the delta
                    // position for the axis.
                    if (Mathf.Abs(DeltaPosition[i]) >= k_MaxDeltaBeforeAdjustment)
                    {
                        CurrentBasePosition[i] += DeltaPosition[i];
                        DeltaPosition[i] = 0.0f;
                        Axis[i] = 0;
                    }
                }
            }
            return CurrentBasePosition + DeltaPosition;
        }

        public void UpdateAxisToSynchronize(HalfVector3AxisToSynchronize halfVector3AxisToSynchronize)
        {
            m_HalfVector3AxisToSynchronize = halfVector3AxisToSynchronize;
        }

        /// <summary>
        /// Returns the base position without the delta
        /// </summary>
        /// <returns><see cref="Vector3"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetCurrentBasePosition()
        {
            return CurrentBasePosition;
        }

        /// <summary>
        /// Returns the full position with the delta
        /// </summary>
        /// <returns><see cref="Vector3"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetFullPosition()
        {
            return CurrentBasePosition + DeltaPosition;
        }

        /// <summary>
        /// The half float vector3 version of the current delta position.
        /// Only applies to the authoritative side for <see cref="NetworkTransform"/>
        /// instances.
        /// </summary>
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
        /// Sets the new position delta based off of the initial position.
        /// </summary>
        /// <param name="vector3">the full <see cref="Vector3"/> to generate a delta from</param>
        /// <param name="tick">the network tick when this Vector3 value is applied</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromVector3(ref Vector3 vector3, int tick)
        {
            NetworkTick = tick;
            DeltaPosition = (vector3 + PrecisionLossDelta) - CurrentBasePosition;

            for (int i = 0; i < 3; i++)
            {
                if (m_HalfVector3AxisToSynchronize.SyncAxis[i])
                {
                    Axis[i] = Mathf.FloatToHalf(DeltaPosition[i]);
                    HalfDeltaConvertedBack[i] = Mathf.HalfToFloat(Axis[i]);
                    PrecisionLossDelta[i] = DeltaPosition[i] - HalfDeltaConvertedBack[i];
                    if (Mathf.Abs(HalfDeltaConvertedBack[i]) >= k_MaxDeltaBeforeAdjustment)
                    {
                        CurrentBasePosition[i] += HalfDeltaConvertedBack[i];
                        HalfDeltaConvertedBack[i] = 0.0f;
                        DeltaPosition[i] = 0.0f;
                    }
                }
            }
            PreviousPosition = vector3;
        }

        /// <summary>
        /// One of two constructors that should be called to set the initial position.
        /// This uses a <see cref="Vector3"/> for initialization.
        /// </summary>
        /// <param name="vector3">the <see cref="Vector3"/> to initialize this instance with</param>
        /// <param name="networkTick">use the network tick when creating</param>
        public HalfVector3DeltaPosition(Vector3 vector3, int networkTick, HalfVector3AxisToSynchronize halfVector3AxisToSynchronize)
        {
            m_HalfVector3AxisToSynchronize = halfVector3AxisToSynchronize;
            Axis = new ushort[3];
            NetworkTick = networkTick;
            CurrentBasePosition = vector3;
            PreviousPosition = vector3;
            PrecisionLossDelta = Vector3.zero;
            DeltaPosition = Vector3.zero;
            HalfDeltaConvertedBack = Vector3.zero;
            FromVector3(ref vector3, networkTick);
        }

        /// <summary>
        /// One of two constructors that should be called to set the initial position.
        /// This uses individual x, y, and z floats for initialization.
        /// </summary>
        /// <param name="x">x-axis value to set</param>
        /// <param name="y">y-axis value to set</param>
        /// <param name="z">z-axis value to set</param>
        /// <param name="networkTick">use the network tick when creating</param>
        public HalfVector3DeltaPosition(float x, float y, float z, int networkTick, HalfVector3AxisToSynchronize halfVector3AxisToSynchronize) :
            this(new Vector3(x, y, z), networkTick, halfVector3AxisToSynchronize)
        {
        }
    }
}
