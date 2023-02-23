
namespace Unity.Netcode.Components
{
    /// <summary>
    /// Structure that defines which axis of a <see cref="HalfVector3"/>
    /// and <see cref="HalfVector3DeltaPosition"/> will be serialized.
    /// </summary>
    public struct HalfVector3AxisToSynchronize
    {
        /// <summary>
        /// When enabled, serialize the X axis
        /// </summary>
        public bool X => SyncAxis[0];

        /// <summary>
        /// When enabled, serialize the Y axis
        /// </summary>
        public bool Y => SyncAxis[1];

        /// <summary>
        /// When enabled, serialize the Z axis
        /// </summary>
        public bool Z => SyncAxis[2];

        public bool[] SyncAxis;

        /// <summary>
        /// Constructor to preinitialize the x, y, and z values
        /// </summary>
        /// <param name="x">when <see cref="true"/> the x-axis will be serialized</param>
        /// <param name="y">when <see cref="true"/> the y-axis will be serialized</param>
        /// <param name="z">when <see cref="true"/> the z-axis will be serialized</param>
        public HalfVector3AxisToSynchronize(bool x = true, bool y = true, bool z = true)
        {
            SyncAxis = new bool[3];
            SyncAxis[0] = x;
            SyncAxis[1] = y;
            SyncAxis[2] = z;
        }
    }
}
