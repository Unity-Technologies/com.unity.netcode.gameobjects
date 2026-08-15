using Unity.Burst;
using Unity.Collections;
using UnityEngine.Jobs;
using static Unity.Netcode.Components.NetworkTransform;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Motion Authority Only:
    /// Detects <see cref="NetworkTransform"/> state changes for every registered instance in parallel.
    /// </summary>
    /// <remarks>
    /// This reads each transform and defers to the very same
    /// <see cref="CheckForStateChange(ref NetworkTransformState, ref NetworkDeltaPosition, ref TransformDeltaConfig, in TransformSample, bool, bool, bool)"/>
    /// that the per instance path runs on the main thread to keep the logic between per instance and batched the same.<br />
    /// Only transform values are read here and only the entries array is written, so there is no hierarchy
    /// write hazard: nothing in this job touches a transform other than the one at its own index, and nothing
    /// writes to a transform at all.
    /// </remarks>
    [BurstCompile]
    internal struct DetectTransformDeltaJob : IJobParallelForTransform
    {
        /// <summary>
        /// The per instance input and output, parallel to the transforms this job is scheduled over.
        /// </summary>
        public NativeArray<TransformDeltaEntry> Entries;

        public void Execute(int index, TransformAccess transform)
        {
            if (!transform.isValid)
            {
                return;
            }

            var entry = Entries[index];
            var flagStates = entry.State.FlagStates;
            var forceState = entry.ForceState;

            // Resolve the transform space before sampling, otherwise the wrong set of values gets compared.
            var transformSpaceChanged = ResolveTransformSpace(ref entry.Config, ref flagStates, entry.TransformHasParent, false, ref forceState);
            entry.State.FlagStates = flagStates;

            // A rigidbody driven instance cannot be sampled from here, so it is never registered for the
            // batched path and always falls back to the per instance flow.
            var rotation = entry.Config.InLocalSpace ? transform.localRotation : transform.rotation;
            entry.Sample.Position = entry.Config.InLocalSpace ? transform.localPosition : transform.position;
            entry.Sample.Rotation = rotation;
            entry.Sample.RotAngles = NetworkTransformMath.EulerAngles(rotation);
            entry.Sample.Scale = transform.localScale;

            entry.IsDirty = CheckForStateChange(ref entry.State, ref entry.HalfPositionState, ref entry.Config,
                entry.Sample, false, forceState, transformSpaceChanged);

            Entries[index] = entry;
        }
    }
}
