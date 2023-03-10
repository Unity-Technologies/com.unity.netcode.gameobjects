using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Half Precision <see cref="Vector4"/> 
    /// </summary>
    /// <remarks>
    /// This can also be used to convert a <see cref="Quaternion"/> to half precision.
    /// </remarks>
    public struct HalfVector4 : INetworkSerializable
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
        /// The half float precision value of the w-axis as a <see cref="ushort"/>.
        /// </summary>
        public ushort W => Axis.W;

        /// <summary>
        /// Used to store the half float precision value as a <see cref="ushort"/>
        /// </summary>
        public Vector4T<ushort> Axis;

        /// <summary>
        /// The serialization implementation of <see cref="INetworkSerializable"/>
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            for (int i = 0; i < Axis.Length; i++)
            {
                var axisValue = Axis[i];
                serializer.SerializeValue(ref axisValue);
                if (serializer.IsReader)
                {
                    Axis[i] = axisValue;
                }
            }
        }

        /// <summary>
        /// Converts this instance to a full precision <see cref="Vector4"/>.
        /// </summary>
        /// <returns>A <see cref="Vector4"/> as the full precision value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 ToVector4()
        {
            Vector4 fullPrecision = Vector4.zero;
            for (int i = 0; i < Axis.Length; i++)
            {
                fullPrecision[i] = Mathf.HalfToFloat(Axis[i]);
            }
            return fullPrecision;
        }

        /// <summary>
        /// Converts this instance to a full precision <see cref="Quaternion"/>.
        /// </summary>
        /// <returns>A <see cref="Quaternion"/> as the full precision value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion ToQuaternion()
        {
            var quaternion = Quaternion.identity;
            for (int i = 0; i < Axis.Length; i++)
            {
                quaternion[i] = Mathf.HalfToFloat(Axis[i]);
            }
            return quaternion;
        }

        /// <summary>
        /// Converts a full precision <see cref="Vector4"/> to half precision and updates the current instance.
        /// </summary>
        /// <param name="vector4">The <see cref="Vector4"/> to convert and update this instance with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateFrom(ref Vector4 vector4)
        {
            for (int i = 0; i < Axis.Length; i++)
            {
                Axis[i] = Mathf.FloatToHalf(vector4[i]);
            }
        }

        /// <summary>
        /// Converts a full precision <see cref="Vector4"/> to half precision and updates the current instance.
        /// </summary>
        /// <param name="quaternion">The <see cref="Quaternion"/> to convert and update this instance with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateFrom(ref Quaternion quaternion)
        {
            for (int i = 0; i < Axis.Length; i++)
            {
                Axis[i] = Mathf.FloatToHalf(quaternion[i]);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vector4">The initial axial values (converted to half floats) when instantiated.</param>
        public HalfVector4(Vector4 vector4)
        {
            Axis = default;
            UpdateFrom(ref vector4);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="x">The initial x axis (converted to half float) value when instantiated.</param>
        /// <param name="y">The initial y axis (converted to half float) value when instantiated.</param>
        /// <param name="z">The initial z axis (converted to half float) value when instantiated.</param>
        /// <param name="w">The initial w axis (converted to half float) value when instantiated.</param>
        public HalfVector4(float x, float y, float z, float w) : this(new Vector4(x, y, z, w))
        {
        }
    }
}
