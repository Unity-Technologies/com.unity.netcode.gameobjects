using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Half float precision <see cref="Vector3"/>
    /// </summary>
    public struct HalfVector3 : INetworkSerializable
    {
        /// <summary>
        /// The half float precision value of the x-axis as a <see cref="ushort"/>
        /// </summary>
        public ushort X => Axis[0];
        /// <summary>
        /// The half float precision value of the y-axis as a <see cref="ushort"/>
        /// </summary>
        public ushort Y => Axis[1];
        /// <summary>
        /// The half float precision value of the z-axis as a <see cref="ushort"/>
        /// </summary>
        public ushort Z => Axis[2];

        public ushort[] Axis;

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
        /// Converts the <see cref="HalfVector3"/> to a full precision <see cref="Vector3"/>
        /// </summary>
        /// <param name="vector3">the <see cref="Vector3"/> to store the full precision values in</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToVector3(ref Vector3 vector3)
        {
            for (int i = 0; i < 3; i++)
            {
                if (m_HalfVector3AxisToSynchronize.SyncAxis[i])
                {
                    vector3[i] = Mathf.HalfToFloat(Axis[i]);
                }
            }
        }

        /// <summary>
        /// Converts a <see cref="Vector3"/> full precision to a <see cref="HalfVector3"/>
        /// </summary>
        /// <param name="vector3">the <see cref="Vector3"/> to convert to a half precision <see cref="Vector3"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromVector3(ref Vector3 vector3)
        {
            for (int i = 0; i < 3; i++)
            {
                if (m_HalfVector3AxisToSynchronize.SyncAxis[i])
                {
                    Axis[i] = Mathf.FloatToHalf(vector3[i]);
                }
            }
        }

        /// <summary>
        /// Constructor that initializes the HalfVector3 along with its precision adjustment
        /// </summary>
        /// <remarks>
        /// Note about decimal precision:
        /// If you know that all components (x, y, and z) of a Vector3 will not exceed a specific threshold, then you
        /// can increase the decimal precision of a Vector3 by increasing the decimalPrecision value. The decimal precision
        /// valid values range from 0 to 4 decimal places to adjust up and back down (1000 to 1 : 1 to 0.001).
        /// </remarks>
        /// <param name="vector3">the vector3 to initialize the HalfVector3 with</param>
        public HalfVector3(Vector3 vector3, HalfVector3AxisToSynchronize halfVector3AxisToSynchronize)
        {
            Axis = new ushort[3];
            m_HalfVector3AxisToSynchronize = halfVector3AxisToSynchronize;
            FromVector3(ref vector3);
        }

        /// <summary>
        /// Constructor that initializes the HalfVector3 along with its precision adjustment
        /// </summary>
        /// <remarks>
        /// Note about decimal precision:
        /// If you know that all components (x, y, and z) of a Vector3 will not exceed a specific threshold, then you
        /// can increase the decimal precision of a Vector3 by increasing the decimalPrecision value. The decimal precision
        /// valid values range from 0 to 4 decimal places to adjust up and back down (1000 to 1 : 1 to 0.001).
        /// </remarks>
        /// <param name="x">x component to initialize the HalfVector3 with</param>
        /// <param name="y">y component of initialize the HalfVector3 with</param>
        /// <param name="z">z component of initialize the HalfVector3 with</param>
        public HalfVector3(float x, float y, float z, HalfVector3AxisToSynchronize halfVector3AxisToSynchronize) : this(new Vector3(x, y, z), halfVector3AxisToSynchronize)
        {
        }
    }
}
