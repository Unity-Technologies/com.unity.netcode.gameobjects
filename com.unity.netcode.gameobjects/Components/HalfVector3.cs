using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode.Components
{
    public struct HalfVector3AxisToSynchronize
    {
        public bool X;
        public bool Y;
        public bool Z;

        public HalfVector3AxisToSynchronize(bool x = true, bool y = true, bool z = true)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct HalfVector3 : INetworkSerializable
    {
        public ushort X;
        public ushort Y;
        public ushort Z;

        private float m_PrecisionAdjustmentUp;
        private float m_PrecisionAdjustmentDown;
        private HalfVector3AxisToSynchronize m_HalfVector3AxisToSynchronize;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (m_HalfVector3AxisToSynchronize.X)
            {
                serializer.SerializeValue(ref X);
            }

            if (m_HalfVector3AxisToSynchronize.Y)
            {
                serializer.SerializeValue(ref Y);
            }

            if (m_HalfVector3AxisToSynchronize.Z)
            {
                serializer.SerializeValue(ref Z);
            }
        }

        /// <summary>
        /// Converts the <see cref="HalfVector3"/> to a full precision <see cref="Vector3"/>
        /// </summary>
        /// <param name="vector3">the <see cref="Vector3"/> to store the full precision values in</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToVector3(ref Vector3 vector3)
        {
            vector3.x = Mathf.HalfToFloat(X);
            vector3.y = Mathf.HalfToFloat(Y);
            vector3.z = Mathf.HalfToFloat(Z);
            vector3 *= m_PrecisionAdjustmentDown;
        }

        /// <summary>
        /// Converts a <see cref="Vector3"/> full precision to a <see cref="HalfVector3"/>
        /// </summary>
        /// <param name="vector3">the <see cref="Vector3"/> to convert to a half precision <see cref="Vector3"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromVector3(ref Vector3 vector3)
        {
            vector3 *= m_PrecisionAdjustmentUp;
            X = Mathf.FloatToHalf(vector3.x);
            Y = Mathf.FloatToHalf(vector3.y);
            Z = Mathf.FloatToHalf(vector3.z);
        }

        public void SetDecimalPrecision(int decimalPrecision)
        {
            decimalPrecision = Mathf.Clamp(decimalPrecision, 0, 4);
            m_PrecisionAdjustmentUp = Mathf.Pow(10.0f, decimalPrecision);
            m_PrecisionAdjustmentDown = Mathf.Pow(10.0f, 1.0f / decimalPrecision);
        }

        /// <summary>
        /// Constructor that initializes the HalfVector3 along with its precision adjustment
        /// </summary>
        /// <remarks>
        /// Note about decimal precision:
        /// If you know that all components (x, y, and z) of a Vector3 will not exceed a specific threshold, then you
        /// can increase the decimal precision of a Vector3 by increasing the decimalPrecision value. The decimal precision
        /// valid values range from 0 to 4.  Where 10 ^ 0 is 1 (i.e. no decimal place adjustment).
        /// Thus 10 ^ 4 is 10000 (4 decimal places to the right) when converting to a half float value, and 10 ^ 1/4 is 0.0001 (4
        /// decimal places to the left) when converting from the half float to full precision float value.
        /// Using 4 decimal places typically should only be used if the Vector3 you are converting to half float precision is normalized.
        /// </remarks>
        /// <param name="vector3">the vector3 to initialize the HalfVector3 with</param>
        /// <param name="decimalPrecision">
        /// The number of decimal places to move the <see cref="Vector3"/> values prior to converting to half precision and back when converting to
        /// a full precision <see cref="Vector3"/>. The default value is 0 (i.e. don't adjust the decimal place).
        /// </param>
        public HalfVector3(Vector3 vector3, HalfVector3AxisToSynchronize halfVector3AxisToSynchronize, int decimalPrecision = 0)
        {
            X = Y = Z = 0;
            m_PrecisionAdjustmentUp = m_PrecisionAdjustmentDown = 0.0f;
            m_HalfVector3AxisToSynchronize = halfVector3AxisToSynchronize;
            SetDecimalPrecision(decimalPrecision);
            FromVector3(ref vector3);
        }

        /// <summary>
        /// Constructor that initializes the HalfVector3 along with its precision adjustment
        /// </summary>
        /// <remarks>
        /// Note about decimal precision:
        /// If you know that all components (x, y, and z) of a Vector3 will not exceed a specific threshold, then you
        /// can increase the decimal precision of a Vector3 by increasing the decimalPrecision value. The decimal precision
        /// valid values range from 0 to 4.  Where 10 ^ 0 is 1 (i.e. no decimal place adjustment).
        /// Thus 10 ^ 4 is 10000 (4 decimal places to the right) when converting to a half float value, and 10 ^ 1/4 is 0.0001 (4
        /// decimal places to the left) when converting from the half float to full precision float value.
        /// Using 4 decimal places typically should only be used if the Vector3 you are converting to half float precision is normalized.
        /// </remarks>
        /// <param name="x">x component to initialize the HalfVector3 with</param>
        /// <param name="y">y component of initialize the HalfVector3 with</param>
        /// <param name="z">z component of initialize the HalfVector3 with</param>
        /// <param name="decimalPrecision">
        /// The number of decimal places to move the <see cref="Vector3"/> values prior to converting to half precision and back when converting to
        /// a full precision <see cref="Vector3"/>. The default value is 0 (i.e. don't adjust the decimal place).
        /// </param>
        public HalfVector3(float x, float y, float z, HalfVector3AxisToSynchronize halfVector3AxisToSynchronize, int decimalPrecision = 0)
        {
            X = Y = Z = 0;
            m_PrecisionAdjustmentUp = m_PrecisionAdjustmentDown = 0.0f;
            m_HalfVector3AxisToSynchronize = halfVector3AxisToSynchronize;
            var vector3 = new Vector3(x, y, z);
            SetDecimalPrecision(decimalPrecision);
            FromVector3(ref vector3);
        }
    }
}
