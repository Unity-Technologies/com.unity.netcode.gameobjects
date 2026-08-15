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
    /// Advances the interpolators for every registered non-authority <see cref="NetworkTransform"/> in
    /// parallel.
    /// </summary>
    /// <remarks>
    /// This performs the buffer consumption and the interpolation math only. Applying the results to the
    /// transforms stays on the main thread for now (keeps this free of any hierarchy write order of operation complexities).<br />
    /// <br />
    /// Also, note that each entry owns its own slice of <see cref="BufferedItems"/> which assures no two indices address the same
    /// items and that the whole array can be written without aliasing (pointing to the same thing).
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
