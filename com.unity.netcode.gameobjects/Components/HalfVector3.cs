using UnityEngine;

namespace Unity.Netcode.Components
{
    public struct HalfVector3 : INetworkSerializable
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

        internal Vector3 FullPosition;
        internal Vector3 PrecisionLossDelta;
        internal Vector3 DeltaPosition;

        /// <summary>
        /// The precision adjustment is used to help improve precision per delta.
        /// These values are not applied when initializing.
        /// </summary>
        /// <remarks>
        /// Because this initializes using full precision and then uses deltas, we can make an
        /// assumption that the rate of change will not exceed more than 645.04 unity space units
        /// per update.
        /// Example: 322.52 * 30 (network ticks per second) yields 9,675.6 unity space units.
        /// This rate of change would place the object well beyond a typical camera clipping
        /// distance and would not be realistic for any type of game that NGO v1.x.x would try
        /// to support.
        /// In order to properly synchronize the "base" full position with both server and owner
        /// authoritative modes, we need to allow the delta to accumulate over time in order to
        /// avoid missing synchronization of the full position which needs to be perfectly set
        /// on all instances. This is why we don't just apply the delta per update but wait for it
        /// to reach a maximum threshold value of half the downward adjusted value.
        /// </remarks>
        private const float k_MaxDeltaBeforeAdjustment = (64504 * k_PrecisionAdjustmentDown) * 0.5f;
        private const float k_PrecisionAdjustmentUp = 100.0f;
        private const float k_PrecisionAdjustmentDown = 0.01f;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref X);
            serializer.SerializeValue(ref Y);
            serializer.SerializeValue(ref Z);
        }

        /// <summary>
        /// Gets the full Vector3 position.
        /// </summary>
        /// <remarks>
        /// !!! Should be only called once per state update !!!
        /// </remarks>
        /// <returns><see cref="Vector3"/></returns>
        public Vector3 ToVector3()
        {
            DeltaPosition.x = Mathf.HalfToFloat(X) * k_PrecisionAdjustmentDown;
            DeltaPosition.y = Mathf.HalfToFloat(Y) * k_PrecisionAdjustmentDown;
            DeltaPosition.z = Mathf.HalfToFloat(Z) * k_PrecisionAdjustmentDown;
            // If we exceed or are equal to the maximum delta value then we need to
            // apply the delta to the full position value and reset the delta position.
            if (DeltaPosition.magnitude >= k_MaxDeltaBeforeAdjustment)
            {
                FullPosition += DeltaPosition;
                DeltaPosition = Vector3.zero;
            }
            // Return the current full position plus the delta position
            return FullPosition + DeltaPosition;
        }

        /// <summary>
        /// Sets the new position delta based off of the initial position.
        /// </summary>
        /// <remarks>
        /// This include precision loss adjustments.
        /// </remarks>
        /// <param name="vector3">the full <see cref="Vector3"/> to generate a delta from</param>
        public void FromVector3(ref Vector3 vector3)
        {
            DeltaPosition = (vector3 + PrecisionLossDelta) - FullPosition;
            X = Mathf.FloatToHalf(DeltaPosition.x * k_PrecisionAdjustmentUp);
            Y = Mathf.FloatToHalf(DeltaPosition.y * k_PrecisionAdjustmentUp);
            Z = Mathf.FloatToHalf(DeltaPosition.z * k_PrecisionAdjustmentUp);
            PrecisionLossDelta = DeltaPosition - new Vector3(Mathf.HalfToFloat(X) * k_PrecisionAdjustmentDown, Mathf.HalfToFloat(Y) * k_PrecisionAdjustmentDown, Mathf.HalfToFloat(Z) * k_PrecisionAdjustmentDown);
            // If we exceed or are equal to the maximum delta value then we need to
            // apply the delta to the full position value and reset the delta position.
            if (DeltaPosition.magnitude >= k_MaxDeltaBeforeAdjustment)
            {
                FullPosition = vector3;
                DeltaPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// One of two constructors that should be called to set the initial position.
        /// </summary>
        public HalfVector3(Vector3 vector3)
        {
            X = 0;
            Y = 0;
            Z = 0;
            FullPosition = vector3;
            PrecisionLossDelta = Vector3.zero;
            DeltaPosition = Vector3.zero;
            FromVector3(ref vector3);
        }

        /// <summary>
        /// One of two constructors that should be called to set the initial position.
        /// </summary>
        public HalfVector3(float x, float y, float z)
        {
            X = 0;
            Y = 0;
            Z = 0;
            var vector3 = new Vector3(x, y, z);
            FullPosition = vector3;
            PrecisionLossDelta = Vector3.zero;
            DeltaPosition = Vector3.zero;
            FromVector3(ref vector3);
        }
    }
}
