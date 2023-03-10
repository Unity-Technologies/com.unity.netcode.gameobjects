using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Half float precision <see cref="Vector3"/>.
    /// </summary>
    public struct HalfVector3 : INetworkSerializable
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

        internal Vector3AxisToSynchronize AxisToSynchronize;

        /// <summary>
        /// The serialization implementation of <see cref="INetworkSerializable"/>.
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
        /// Gets the full precision value as a <see cref="Vector3"/>.
        /// </summary>
        /// <returns>a <see cref="Vector3"/> as the full precision value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 ToVector3()
        {
            Vector3 fullPrecision = Vector3.zero;
            for (int i = 0; i < Axis.Length; i++)
            {
                if (AxisToSynchronize.SyncAxis[i])
                {
                    fullPrecision[i] = Mathf.HalfToFloat(Axis[i]);
                }
            }
            return fullPrecision;
        }

        /// <summary>
        /// Converts a full precision <see cref="Vector3"/> to half precision and updates the current instance.
        /// </summary>
        /// <param name="vector3">The <see cref="Vector3"/> to convert.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateFrom(ref Vector3 vector3)
        {
            for (int i = 0; i < Axis.Length; i++)
            {
                if (AxisToSynchronize.SyncAxis[i])
                {
                    Axis[i] = Mathf.FloatToHalf(vector3[i]);
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vector3">The initial axial values (converted to half floats) when instantiated.</param>
        /// <param name="vector3AxisToSynchronize">The axis to synchronize.</param>
        public HalfVector3(Vector3 vector3, Vector3AxisToSynchronize vector3AxisToSynchronize)
        {
            Axis = default;
            AxisToSynchronize = vector3AxisToSynchronize;
            UpdateFrom(ref vector3);
        }

        /// <summary>
        /// Constructor that defaults to all axis being synchronized.
        /// </summary>
        /// <param name="vector3">The initial axial values (converted to half floats) when instantiated.</param>
        public HalfVector3(Vector3 vector3) : this(vector3, Vector3AxisToSynchronize.AllAxis)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="x">The initial x axis (converted to half float) value when instantiated.</param>
        /// <param name="y">The initial y axis (converted to half float) value when instantiated.</param>
        /// <param name="z">The initial z axis (converted to half float) value when instantiated.</param>
        /// <param name="vector3AxisToSynchronize">The axis to synchronize.</param>
        public HalfVector3(float x, float y, float z, Vector3AxisToSynchronize vector3AxisToSynchronize) : this(new Vector3(x, y, z), vector3AxisToSynchronize)
        {
        }

        /// <summary>
        /// Constructor that defaults to all axis being synchronized.
        /// </summary>
        /// <param name="x">The initial x axis (converted to half float) value when instantiated.</param>
        /// <param name="y">The initial y axis (converted to half float) value when instantiated.</param>
        /// <param name="z">The initial z axis (converted to half float) value when instantiated.</param>
        public HalfVector3(float x, float y, float z) : this(new Vector3(x, y, z), Vector3AxisToSynchronize.AllAxis)
        {
        }
    }
}
