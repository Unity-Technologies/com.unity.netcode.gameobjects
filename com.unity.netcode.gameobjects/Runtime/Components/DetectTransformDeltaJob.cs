using Unity.Burst;
using Unity.Collections;
using UnityEngine.Jobs;
using static Unity.Netcode.Components.NetworkTransform;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Motion Authority Only:
    /// Detects <see cref="NetworkTransform"/> state changes for every registered instance in a parallel job.
    /// </summary>
    /// <remarks>
    /// <see cref="CheckForStateChange(ref NetworkTransformState, ref NetworkDeltaPosition, ref TransformDeltaConfig, in TransformSample, bool, bool, bool)"/>
    /// is the common method used for both per instance, runs on the main thread, and batched modes. This assures both paths detect changes in state identically.
    /// </remarks>
    [BurstCompile]
    internal struct DetectTransformDeltaJob : IJobParallelForTransform
    {
        /// <summary>
        /// The per instance input and output, parallel to the job's scheduled transforms.
        /// </summary>
        public NativeArray<TransformDeltaEntry> Entries;

        /// <summary>
        /// This job's primary entry point.
        /// </summary>
        /// <remarks>
        /// TODO: Investigate ways to work around the fact that a Rigidbody's position and rotation cannot
        /// be sampled from a job. As such, any NetworkTransform that is using Rigidbody for motion will
        /// use the <see cref="UnityEngine.GameObject.transform"/> to detect changes in position and rotation
        /// states on the authority side.
        /// </remarks>
        /// <param name="index">Index for the transform in question.</param>
        /// <param name="transform">The job safe <see cref="TransformAccess"/></param>
        public void Execute(int index, TransformAccess transform)
        {
            if (!transform.isValid)
            {
                return;
            }

            var entry = Entries[index];
            var flagStates = entry.State.FlagStates;
            // Only ResolveTransformSpace can raise this on the batched path. The one caller that forces a full
            // state update does so from CommitDetectedState on the main thread, after this job has run.
            var forceState = false;

            // Resolve the transform space before sampling, otherwise the wrong set of values gets compared.
            var transformSpaceChanged = ResolveTransformSpace(ref entry.Config, ref flagStates, entry.TransformHasParent, false, ref forceState);
            entry.State.FlagStates = flagStates;

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
