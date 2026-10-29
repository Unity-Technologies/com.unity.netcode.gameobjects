using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// The NGO non-authority instance's transform state used by <see cref="InterpolateTransformJob"/>.
    /// See also:
    /// - <see cref="NativeInterpolator"/>
    /// - <see cref="NativeInterpolatorState"/>
    /// </summary>
    internal struct InterpolationEntry
    {
        internal NativeInterpolatorState Position;
        internal NativeInterpolatorState Rotation;
        internal NativeInterpolatorState Scale;

        /// <summary>
        /// The delta frame time whether fixed or standard delta.
        /// </summary>
        internal float DeltaTime;

        /// <summary>
        /// The "ticks ago" time used to decide which buffered measurements are ready to consume.
        /// </summary>
        internal double TickLatencyAsTime;

        /// <summary>
        /// The render time used by <see cref="NetworkTransform.InterpolationTypes.LegacyLerp"/> only.
        /// </summary>
        internal double LegacyRenderTime;

        internal double CurrentTime;
        internal double MinDeltaTime;
        internal double MaxDeltaTime;

        internal NetworkTransform.InterpolationTypes PositionInterpolationType;
        internal NetworkTransform.InterpolationTypes RotationInterpolationType;
        internal NetworkTransform.InterpolationTypes ScaleInterpolationType;

        internal bool SynchronizePosition;
        internal bool SynchronizeRotation;
        internal bool SynchronizeScale;

        // Results, read back on the main thread and applied to the transform there.
        internal float4 InterpolatedPosition;
        internal float4 InterpolatedRotation;
        internal float4 InterpolatedScale;
    }

    /// <summary>
    /// Non-Authority Only:
    /// Handles interpolation for every registered non-authority <see cref="NetworkTransform"/> in
    /// a parallel job.
    /// </summary>
    /// <remarks>
    /// This performs the buffer consumption and interpolation between two state updates only.<br />
    /// Applying the results to the transforms stays on the main thread, which keeps this job free
    /// of hierarchy write ordering.<br />
    /// Each entry owns its own slice of <see cref="BufferedItems"/>, so no two indices address the same items.<br />
    /// The whole array can be written without aliasing (access is to a distinct, independent memory region).
    /// </remarks>
    [BurstCompile]
    internal struct InterpolateTransformJob : IJobParallelFor
    {
        public NativeArray<InterpolationEntry> Entries;

        /// <summary>
        /// The shared state measurement storage. Disabling the safety restriction is what allows each index to write
        /// into its own slice of one array; <see cref="NativeInterpolatorState.BufferOffset"/> keeps those
        /// slices disjoint.
        /// </summary>
        [NativeDisableParallelForRestriction]
        public NativeArray<BufferedItemNative> BufferedItems;

        public void Execute(int index)
        {
            var entry = Entries[index];

            if (entry.SynchronizePosition)
            {
                entry.InterpolatedPosition = Advance(ref entry.Position, ref entry, entry.PositionInterpolationType);
            }

            if (entry.SynchronizeRotation)
            {
                entry.InterpolatedRotation = Advance(ref entry.Rotation, ref entry, entry.RotationInterpolationType);
            }

            if (entry.SynchronizeScale)
            {
                entry.InterpolatedScale = Advance(ref entry.Scale, ref entry, entry.ScaleInterpolationType);
            }

            Entries[index] = entry;
        }

        private float4 Advance(ref NativeInterpolatorState state, ref InterpolationEntry entry, NetworkTransform.InterpolationTypes interpolationType)
        {
            if (interpolationType == NetworkTransform.InterpolationTypes.LegacyLerp)
            {
                return NativeInterpolator.UpdateLegacy(ref state, ref BufferedItems, entry.DeltaTime, entry.LegacyRenderTime, entry.CurrentTime);
            }

            return NativeInterpolator.Update(ref state, ref BufferedItems, entry.DeltaTime, entry.TickLatencyAsTime,
                entry.MinDeltaTime, entry.MaxDeltaTime, interpolationType == NetworkTransform.InterpolationTypes.Lerp);
        }
    }
}
