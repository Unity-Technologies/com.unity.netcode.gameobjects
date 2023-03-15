
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Structure that defines which axis of a <see cref="Vector3"/> will be serialized.
    /// </summary>
    public struct Vector3AxisToSynchronize
    {

        public static Vector3AxisToSynchronize AllAxis = new Vector3AxisToSynchronize();
        /// <summary>
        /// When enabled, serialize the X axis
        /// </summary>
        public bool X => SyncAxis.X;

        /// <summary>
        /// When enabled, serialize the Y axis
        /// </summary>
        public bool Y => SyncAxis.Y;

        /// <summary>
        /// When enabled, serialize the Z axis
        /// </summary>
        public bool Z => SyncAxis.Z;

        /// <summary>
        /// Used to store the axis flag value as a <see cref="bool"/>
        /// </summary>
        public Vector3T<bool> SyncAxis;

        /// <summary>
        /// Constructor to preinitialize the x, y, and z values
        /// </summary>
        /// <param name="x">when <see cref="true"/> the x-axis will be serialized</param>
        /// <param name="y">when <see cref="true"/> the y-axis will be serialized</param>
        /// <param name="z">when <see cref="true"/> the z-axis will be serialized</param>
        public Vector3AxisToSynchronize(bool x = true, bool y = true, bool z = true)
        {
            SyncAxis = default;
            SyncAxis[0] = x;
            SyncAxis[1] = y;
            SyncAxis[2] = z;
        }
    }
}
