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

        public Vector3 CurrentBasePosition;
        public Vector3 InternalFullPosition;
        internal Vector3 PrecisionLossDelta;
        public Vector3 DeltaPosition;

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
        private const float k_MaxDeltaBeforeAdjustment = 32.0f;// (64504 * k_PrecisionAdjustmentDown);
        private const float k_PrecisionAdjustmentUp = 1.0f;
        private const float k_PrecisionAdjustmentDown = 1.0f;

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

        public Vector3 GetFullPosition()
        {
            return CurrentBasePosition + DeltaPosition;
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


            /// !!!!!!!!!!!!!!! CURRENT ISSUE !!!!!!!!!!!!!!!!!!!
            /// With the below changes we are back to the issue with out of synch deltas
            /// "Whack-A-Mole!!!!"
            DeltaPosition = (vector3 + PrecisionLossDelta) - CurrentBasePosition;

            X = Mathf.FloatToHalf(DeltaPosition.x * k_PrecisionAdjustmentUp);
            Y = Mathf.FloatToHalf(DeltaPosition.y * k_PrecisionAdjustmentUp);
            Z = Mathf.FloatToHalf(DeltaPosition.z * k_PrecisionAdjustmentUp);

            var deltaBack = new Vector3(Mathf.HalfToFloat(X) * k_PrecisionAdjustmentDown, Mathf.HalfToFloat(Y) * k_PrecisionAdjustmentDown, Mathf.HalfToFloat(Z) * k_PrecisionAdjustmentDown);
            PrecisionLossDelta = DeltaPosition - deltaBack;

            if (Mathf.Abs(deltaBack.x) >= k_MaxDeltaBeforeAdjustment)
            {
                CurrentBasePosition.x += deltaBack.x;
            }

            if (Mathf.Abs(deltaBack.y) >= k_MaxDeltaBeforeAdjustment)
            {
                CurrentBasePosition.y += deltaBack.y;
            }

            if (Mathf.Abs(deltaBack.z) >= k_MaxDeltaBeforeAdjustment)
            {
                CurrentBasePosition.z += deltaBack.z;
            }

            InternalFullPosition = vector3;
        }

        /// <summary>
        /// One of two constructors that should be called to set the initial position.
        /// </summary>
        public HalfVector3(Vector3 vector3)
        {
            X = Y = Z = 0;
            CurrentBasePosition = vector3;
            InternalFullPosition = vector3;
            PrecisionLossDelta = Vector3.zero;
            DeltaPosition = Vector3.zero;

            FromVector3(ref vector3);
        }

        /// <summary>
        /// One of two constructors that should be called to set the initial position.
        /// </summary>
        public HalfVector3(float x, float y, float z)
        {
            X = Y = Z = 0;
            var vector3 = new Vector3(x, y, z);
            CurrentBasePosition = vector3;
            InternalFullPosition = vector3;
            PrecisionLossDelta = Vector3.zero;
            DeltaPosition = Vector3.zero;

            FromVector3(ref vector3);
        }
    }
}
