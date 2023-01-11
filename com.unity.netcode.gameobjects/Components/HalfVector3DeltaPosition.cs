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
        /// The Half Float Delta Position X Value
        /// </summary>
        public ushort X;
        /// <summary>
        /// The Half Float Delta Position Y Value
        /// </summary>
        public ushort Y;
        /// <summary>
        /// The Half Float Delta Position Z Value
        /// </summary>
        public ushort Z;

        public Vector3 CurrentBasePosition;

        internal Vector3 PrecisionLossDelta;
        internal Vector3 HalfDeltaConvertedBack;
        internal Vector3 PreviousPosition;
        internal Vector3 DeltaPosition;
        internal int NetworkTick;

        private const float k_MaxDeltaBeforeAdjustment = 128.0f;
        private const float k_AdjustmentUp = 100.0f;
        private const float k_AdjustmentDown = 0.01f;

        /// <summary>
        /// The serialization implementation of <see cref="INetworkSerializable"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref X);
            serializer.SerializeValue(ref Y);
            serializer.SerializeValue(ref Z);
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

            DeltaPosition.x = Mathf.HalfToFloat(X) * k_AdjustmentDown;
            DeltaPosition.y = Mathf.HalfToFloat(Y) * k_AdjustmentDown;
            DeltaPosition.z = Mathf.HalfToFloat(Z) * k_AdjustmentDown;

            // If we exceed or are equal to the maximum delta value then we need to
            // apply the delta to the CurrentBasePosition value and reset the delta
            // position for the axis.
            if (Mathf.Abs(DeltaPosition.x) >= k_MaxDeltaBeforeAdjustment)
            {
                CurrentBasePosition.x += DeltaPosition.x;
                DeltaPosition.x = 0.0f;
                X = 0;
            }

            if (Mathf.Abs(DeltaPosition.y) >= k_MaxDeltaBeforeAdjustment)
            {
                CurrentBasePosition.y += DeltaPosition.y;
                DeltaPosition.y = 0.0f;
                Y = 0;
            }

            if (Mathf.Abs(DeltaPosition.z) >= k_MaxDeltaBeforeAdjustment)
            {
                CurrentBasePosition.z += DeltaPosition.z;
                DeltaPosition.z = 0.0f;
                Z = 0;
            }

            return CurrentBasePosition + DeltaPosition;
        }

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

            X = Mathf.FloatToHalf(DeltaPosition.x * k_AdjustmentUp);
            Y = Mathf.FloatToHalf(DeltaPosition.y * k_AdjustmentUp);
            Z = Mathf.FloatToHalf(DeltaPosition.z * k_AdjustmentUp);

            HalfDeltaConvertedBack.x = Mathf.HalfToFloat(X) * k_AdjustmentDown;
            HalfDeltaConvertedBack.y = Mathf.HalfToFloat(Y) * k_AdjustmentDown;
            HalfDeltaConvertedBack.z = Mathf.HalfToFloat(Z) * k_AdjustmentDown;

            PrecisionLossDelta = DeltaPosition - HalfDeltaConvertedBack;

            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(HalfDeltaConvertedBack[i]) >= k_MaxDeltaBeforeAdjustment)
                {
                    CurrentBasePosition[i] += HalfDeltaConvertedBack[i];
                    HalfDeltaConvertedBack[i] = 0.0f;
                    DeltaPosition[i] = 0.0f;
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
        public HalfVector3DeltaPosition(Vector3 vector3, int networkTick)
        {
            X = Y = Z = 0;
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
        public HalfVector3DeltaPosition(float x, float y, float z, int networkTick)
        {
            X = Y = Z = 0;
            NetworkTick = networkTick;
            var vector3 = new Vector3(x, y, z);
            CurrentBasePosition = vector3;
            PreviousPosition = vector3;
            PrecisionLossDelta = Vector3.zero;
            DeltaPosition = Vector3.zero;
            HalfDeltaConvertedBack = Vector3.zero;
            FromVector3(ref vector3, networkTick);
        }
    }
}
