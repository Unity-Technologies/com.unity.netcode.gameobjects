using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Netcode.Components
{
    public partial class NetworkTransform
    {
        /// <summary>
        /// Abstraction layer config:
        /// Everything <see cref="CheckForStateChange"/> needs from the instance it is checking.
        /// </summary>
        /// <remarks>
        /// This creates the abstraction layer between <see cref="NetworkTransform"/> itself and the configuration
        /// of the <see cref="NetworkTransform"/> in order to assure the delta check can be run both on the main
        /// thread and from within a job.<br />
        /// A few members are read/write: the check can change the transform space it operates in and it
        /// advances the axial frame synchronization bookkeeping, both of which have to make it back to the
        /// instance.
        /// </remarks>
        internal struct TransformDeltaConfig
        {
            internal float PositionThreshold;
            internal float RotAngleThreshold;
            internal float ScaleThreshold;

            internal bool SyncPositionX;
            internal bool SyncPositionY;
            internal bool SyncPositionZ;
            internal bool SyncRotAngleX;
            internal bool SyncRotAngleY;
            internal bool SyncRotAngleZ;
            internal bool SyncScaleX;
            internal bool SyncScaleY;
            internal bool SyncScaleZ;

            internal bool UseQuaternionSynchronization;
            internal bool UseQuaternionCompression;
            internal bool UseHalfFloatPrecision;
            internal bool SlerpPosition;
            internal bool Interpolate;
            internal bool UseUnreliableDeltas;
            internal bool SwitchTransformSpaceWhenParented;
            internal bool UseRigidbodyForMotion;

            /// <summary>
            /// Read/write. <see cref="ResolveTransformSpace"/> can change this when
            /// <see cref="SwitchTransformSpaceWhenParented"/> is enabled.
            /// </summary>
            internal bool InLocalSpace;

            internal int CurrentTick;
            internal int CachedTickRate;
            internal int HalfFloatTargetTickOwnership;

            /// <summary>
            /// Read/write. The tick slot this instance next sends an axial frame synchronization on.
            /// </summary>
            internal int NextTickSync;

            /// <summary>
            /// Read/write. Whether a delta has been sent since the last axial frame synchronization.
            /// </summary>
            internal bool DeltaSynch;

            /// <summary>
            /// Some integration and unit tests disable the <see cref="NetworkTransform"/>, in which case the
            /// network tick is not applied to the state.
            /// </summary>
            internal bool Enabled;

            /// <summary>
            /// Write only. Set when the synchronization path produced a state that the (debug only) log entry
            /// handler should be given. Reported back as opposed to being invoked inline so that the delta
            /// check itself stays free of anything that cannot run within a job.
            /// </summary>
            internal bool LogSynchronizationEntry;
        }

        /// <summary>
        /// Abstraction layer struct:
        /// The transform values <see cref="CheckForStateChange"/> compares against, along with the handful of
        /// lookups that can only be resolved on the main thread.
        /// </summary>
        /// <remarks>
        /// The position and rotation are already resolved for local versus world space and for whether a
        /// rigidbody is driving the motion, so the delta check never has to touch a
        /// <see cref="Transform"/> or a rigidbody itself.
        /// </remarks>
        internal struct TransformSample
        {
            internal Vector3 Position;
            internal Quaternion Rotation;
            internal Vector3 RotAngles;
            internal Vector3 Scale;
            internal Vector3 LossyScale;

            /// <summary>
            /// Whether the associated <see cref="NetworkObject"/> is considered parented. Resolving this needs
            /// a <see cref="Component"/> lookup, so it is passed in already resolved.
            /// </summary>
            internal bool HasParentNetworkObject;

            /// <summary>
            /// Synchronization only. The result of <see cref="ShouldSynchronizeHalfFloat"/> for the client
            /// being synchronized.
            /// </summary>
            internal bool ShouldSynchronizeHalfFloat;

            /// <summary>
            /// Synchronization only. When set, the half float delta uses the converted back value as opposed
            /// to the full precision delta position.
            /// </summary>
            internal bool UseHalfDeltaConvertedBack;
        }

        /// <summary>
        /// Abstraction layer struct:
        /// Everything the batched delta check reads and writes for a single <see cref="NetworkTransform"/>.
        /// </summary>
        /// <remarks>
        /// Held as one struct (as opposed to several parallel native arrays) so that adding or removing an
        /// instance only ever has to keep three collections in step rather than a growing number of them.
        /// </remarks>
        internal struct TransformDeltaEntry
        {
            /// <summary>
            /// The last sent state, updated in place by the delta check.
            /// </summary>
            internal NetworkTransformState State;

            /// <summary>
            /// The instance's <see cref="NetworkDeltaPosition"/>, updated in place by the delta check.
            /// </summary>
            internal NetworkDeltaPosition HalfPositionState;

            internal TransformDeltaConfig Config;

            /// <summary>
            /// The parts of the sample that can only be resolved on the main thread. The job fills in the
            /// transform values it reads through the <see cref="UnityEngine.Jobs.TransformAccess"/>.
            /// </summary>
            internal TransformSample Sample;

            /// <summary>
            /// Whether the transform currently has a parent, resolved on the main thread since a job cannot
            /// walk the hierarchy.
            /// </summary>
            internal bool TransformHasParent;

            /// <summary>
            /// Result. Set by the job when there is a state update to send.
            /// </summary>
            internal bool IsDirty;
        }

        /// <summary>
        /// Abstraction Layer Method:
        /// Resolves which transform space the delta check operates in.
        /// </summary>
        /// <remarks>
        /// Runs before the transform is sampled because it determines whether the local or the world values
        /// are the ones being compared. Kept separate (as opposed to being folded into
        /// <see cref="CheckForStateChange"/>) so that neither caller has to sample both spaces.
        /// </remarks>
        /// <param name="config">The instance configuration. <see cref="TransformDeltaConfig.InLocalSpace"/> may be updated.</param>
        /// <param name="flagStates">The state flags being updated.</param>
        /// <param name="transformHasParent">Whether the transform currently has a parent.</param>
        /// <param name="isSynchronization">Whether this is the initial synchronization of the state.</param>
        /// <param name="forceState">Set when the resulting state update has to be a full one.</param>
        /// <returns>true when the transform space changed, which makes the state dirty on its own.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ResolveTransformSpace(ref TransformDeltaConfig config, ref FlagStates flagStates, bool transformHasParent, bool isSynchronization, ref bool forceState)
        {
            // All of the checks below, up to the delta position checking portion, are to determine if the
            // authority changed a property during runtime that requires a full synchronizing.
#if COM_UNITY_MODULES_PHYSICS || COM_UNITY_MODULES_PHYSICS2D
            if (config.UseRigidbodyForMotion || (config.InLocalSpace == flagStates.InLocalSpace && !isSynchronization))
            {
                return false;
            }
#else
            if (config.InLocalSpace == flagStates.InLocalSpace)
            {
                return false;
            }
#endif

            // When SwitchTransformSpaceWhenParented is set we automatically set our local space based on whether
            // we are parented or not.
            flagStates.InLocalSpace = config.SwitchTransformSpaceWhenParented ? transformHasParent : config.InLocalSpace;
            if (config.SwitchTransformSpaceWhenParented)
            {
                config.InLocalSpace = flagStates.InLocalSpace;
            }

            // If we are already teleporting preserve the teleport flag.
            // If we don't have SwitchTransformSpaceWhenParented set or we are synchronizing,
            // then set the teleport flag.
            flagStates.IsTeleportingNextFrame |= !config.SwitchTransformSpaceWhenParented || isSynchronization;

            // Otherwise, if SwitchTransformSpaceWhenParented is set we force a full state update.
            // If interpolation is enabled, then any non-authority instance will update any pending
            // buffered values to the correct world or local space values.
            forceState = config.SwitchTransformSpaceWhenParented;
            return true;
        }

        /// <summary>
        /// Abstraction Layer Method:
        /// Determines whether the sampled transform differs from the last state that was sent.
        /// </summary>
        /// <remarks>
        /// This is primary delta check implementation. <br />
        /// When running in per-instance mode, <see cref="NetworkTransform"/> invokes this on a per instance basis. <br />
        /// When running in batched synchronization mode, this is invoked from within the job.
        /// </remarks>
        /// <param name="networkState">The last sent state, updated in place with any changes.</param>
        /// <param name="halfPositionState">The instance's <see cref="NetworkDeltaPosition"/>, updated in place.</param>
        /// <param name="config">The instance configuration, some of which is updated in place.</param>
        /// <param name="sample">The sampled transform values.</param>
        /// <param name="isSynchronization">Whether this is the initial synchronization of the state.</param>
        /// <param name="forceState">Whether a full state update is being forced.</param>
        /// <param name="transformSpaceChanged">The result of <see cref="ResolveTransformSpace"/>.</param>
        /// <returns>true when there is a state update to send.</returns>
        internal static bool CheckForStateChange(ref NetworkTransformState networkState, ref NetworkDeltaPosition halfPositionState,
            ref TransformDeltaConfig config, in TransformSample sample, bool isSynchronization, bool forceState, bool transformSpaceChanged)
        {
            var flagStates = networkState.FlagStates;

            // As long as we are not doing our first synchronization and we are sending unreliable deltas, each
            // NetworkTransform will stagger its full transfom synchronization over a 1 second period based on the
            // assigned tick slot (m_TickSync).
            // More about DeltaSynch:
            // If we have not sent any deltas since our last frame synch, then this will prevent us from sending
            // frame synch's when the object is at rest. If this is false and a state update is detected and sent,
            // then it will be set to true and each subsequent tick will do this check to determine if it should
            // send a full frame synch.
            var isAxisSync = false;
            // We compare against the NetworkTickSystem version since ServerTime is set when updating ticks
            if (config.UseUnreliableDeltas && !isSynchronization && config.DeltaSynch && config.NextTickSync <= config.CurrentTick)
            {
                // Increment to the next frame synch tick position for this instance
                config.NextTickSync += config.CachedTickRate;
                // If we are teleporting, we do not need to send a frame synch for this tick slot
                // as a "frame synch" really is effectively just a teleport.
                isAxisSync = !flagStates.IsTeleportingNextFrame;
                // Reset our delta synch trigger so we don't send another frame synch until we
                // send at least 1 unreliable state update after this fame synch or teleport
                config.DeltaSynch = false;
            }

            // This is used to determine if we need to send the state update reliably (if we are doing an axial sync)
            flagStates.UnreliableFrameSync = isAxisSync;

            var isTeleportingAndNotSynchronizing = flagStates.IsTeleportingNextFrame && !isSynchronization;
            // The transform space changing is a state change on its own.
            var isDirty = transformSpaceChanged;
            var isPositionDirty = isTeleportingAndNotSynchronizing ? flagStates.HasPositionChange : false;
            var isRotationDirty = isTeleportingAndNotSynchronizing ? flagStates.HasRotAngleChange : false;
            var isScaleDirty = isTeleportingAndNotSynchronizing ? flagStates.HasScaleChange : false;

            flagStates.SwitchTransformSpaceWhenParented = config.SwitchTransformSpaceWhenParented;

            var position = sample.Position;
            var rotation = sample.Rotation;
            var rotAngles = sample.RotAngles;
            var scale = sample.Scale;
            var positionThreshold = config.PositionThreshold;
            var rotationThreshold = config.RotAngleThreshold;

            var synchronizePosition = config.SyncPositionX || config.SyncPositionY || config.SyncPositionZ;
            var synchronizeRotation = config.SyncRotAngleX || config.SyncRotAngleY || config.SyncRotAngleZ;
            var synchronizeScale = config.SyncScaleX || config.SyncScaleY || config.SyncScaleZ;

            flagStates.IsSynchronizing = isSynchronization;

            // Check for parenting when synchronizing and/or teleporting
            if (isSynchronization || flagStates.IsTeleportingNextFrame || forceState)
            {
                // This all has to do with complex nested hierarchies and how it impacts scale
                // when set for the first time or teleporting and depends upon whether the
                // NetworkObject is parented (or "de-parented") at the same time any scale
                // values are applied.
                flagStates.IsParented = sample.HasParentNetworkObject;
            }

            if (config.Interpolate != flagStates.UseInterpolation)
            {
                flagStates.UseInterpolation = config.Interpolate;
                isDirty = true;
                // When we change from interpolating to not interpolating (or vice versa) we need to synchronize/reset everything
                flagStates.IsTeleportingNextFrame = true;
            }

            if (config.UseQuaternionSynchronization != flagStates.QuaternionSync)
            {
                flagStates.QuaternionSync = config.UseQuaternionSynchronization;
                isDirty = true;
                flagStates.IsTeleportingNextFrame = true;
            }

            if (config.UseQuaternionCompression != flagStates.QuaternionCompression)
            {
                flagStates.QuaternionCompression = config.UseQuaternionCompression;
                isDirty = true;
                flagStates.IsTeleportingNextFrame = true;
            }

            if (config.UseHalfFloatPrecision != flagStates.UseHalfFloatPrecision)
            {
                flagStates.UseHalfFloatPrecision = config.UseHalfFloatPrecision;
                isDirty = true;
                flagStates.IsTeleportingNextFrame = true;
            }

            if (config.SlerpPosition != flagStates.UsePositionSlerp)
            {
                flagStates.UsePositionSlerp = config.SlerpPosition;
                isDirty = true;
                flagStates.IsTeleportingNextFrame = true;
            }

            if (config.UseUnreliableDeltas != flagStates.UseUnreliableDeltas)
            {
                flagStates.UseUnreliableDeltas = config.UseUnreliableDeltas;
                isDirty = true;
                flagStates.IsTeleportingNextFrame = true;
            }

            // Begin delta checks against last sent state update
            if (!config.UseHalfFloatPrecision)
            {
                if (config.SyncPositionX && (math.abs(networkState.PositionX - position.x) >= positionThreshold || flagStates.IsTeleportingNextFrame || isAxisSync || forceState))
                {
                    networkState.PositionX = position.x;
                    flagStates.SetHasPosition(Axis.X, true);
                    isPositionDirty = true;
                }

                if (config.SyncPositionY && (math.abs(networkState.PositionY - position.y) >= positionThreshold || flagStates.IsTeleportingNextFrame || isAxisSync || forceState))
                {
                    networkState.PositionY = position.y;
                    flagStates.SetHasPosition(Axis.Y, true);
                    isPositionDirty = true;
                }

                if (config.SyncPositionZ && (math.abs(networkState.PositionZ - position.z) >= positionThreshold || flagStates.IsTeleportingNextFrame || isAxisSync || forceState))
                {
                    networkState.PositionZ = position.z;
                    flagStates.SetHasPosition(Axis.Z, true);
                    isPositionDirty = true;
                }
            }
            else if (synchronizePosition)
            {
                // If we are teleporting then we can skip the delta threshold check
                isPositionDirty = flagStates.IsTeleportingNextFrame || isAxisSync || forceState;
                if (config.HalfFloatTargetTickOwnership > config.CurrentTick)
                {
                    isPositionDirty = true;
                }

                // For NetworkDeltaPosition, if any axial value is dirty then we always send a full update.
                // Unrolled (as opposed to indexing into the Vector3s) since the indexer is a bounds checked
                // property as opposed to a direct field access.
                if (!isPositionDirty)
                {
                    var previousPosition = halfPositionState.PreviousPosition;
                    isPositionDirty = (config.SyncPositionX && math.abs(position.x - previousPosition.x) >= positionThreshold)
                        || (config.SyncPositionY && math.abs(position.y - previousPosition.y) >= positionThreshold)
                        || (config.SyncPositionZ && math.abs(position.z - previousPosition.z) >= positionThreshold);
                }

                // If the position is dirty or we are teleporting (which includes synchronization)
                // then determine what parts of the NetworkDeltaPosition should be updated
                if (isPositionDirty)
                {
                    var axisToSynchronize = math.bool3(config.SyncPositionX, config.SyncPositionY, config.SyncPositionZ);

                    // If we are not synchronizing the transform state for the first time
                    if (!isSynchronization)
                    {
                        // With global teleporting (broadcast to all non-authority instances)
                        // we re-initialize authority's NetworkDeltaPosition and synchronize all
                        // non-authority instances with the new full precision position
                        if (flagStates.IsTeleportingNextFrame)
                        {
                            halfPositionState = new NetworkDeltaPosition(position, networkState.NetworkTick, axisToSynchronize);
                            networkState.CurrentPosition = position;
                        }
                        else // Otherwise, just synchronize the delta position value
                        {
                            halfPositionState.HalfVector3.AxisToSynchronize = axisToSynchronize;
                            halfPositionState.UpdateFrom(ref position, networkState.NetworkTick);
                        }

                        networkState.NetworkDeltaPosition = halfPositionState;

                        // If ownership offset is greater or we are doing an axial synchronization then synchronize the base position
                        if ((config.HalfFloatTargetTickOwnership > config.CurrentTick || isAxisSync) && !flagStates.IsTeleportingNextFrame)
                        {
                            flagStates.SynchronizeBaseHalfFloat = true;
                        }
                        else
                        {
                            flagStates.SynchronizeBaseHalfFloat = config.UseUnreliableDeltas ? halfPositionState.CollapsedDeltaIntoBase : false;
                        }
                    }
                    else // If synchronizing is set, then use the current full position value on the server side
                    {
                        if (sample.ShouldSynchronizeHalfFloat)
                        {
                            // If we have a NetworkDeltaPosition that has a state applied, then we want to determine
                            // what needs to be synchronized. For owner authoritative mode, the server side
                            // will have no valid state yet.
                            if (halfPositionState.NetworkTick > 0)
                            {
                                // Always synchronize the base position and the ushort values of the
                                // current halfPositionState
                                networkState.CurrentPosition = halfPositionState.CurrentBasePosition;
                                networkState.NetworkDeltaPosition = halfPositionState;
                                // If the server is the owner, in both server and owner authoritative modes,
                                // or we are running in server authoritative mode, then we use the
                                // HalfDeltaConvertedBack value as the delta position
                                if (sample.UseHalfDeltaConvertedBack)
                                {
                                    networkState.DeltaPosition = halfPositionState.HalfDeltaConvertedBack;
                                }
                                else
                                {
                                    // Otherwise, we are in owner authoritative mode and the server's NetworkDeltaPosition
                                    // state is "non-authoritative" relative so we use the DeltaPosition.
                                    networkState.DeltaPosition = halfPositionState.DeltaPosition;
                                }
                            }
                            else // Reset everything and just send the current position
                            {
                                networkState.NetworkDeltaPosition = new NetworkDeltaPosition(Vector3.zero, 0, axisToSynchronize);
                                networkState.DeltaPosition = Vector3.zero;
                                networkState.CurrentPosition = position;
                            }
                        }
                        else
                        {
                            networkState.NetworkDeltaPosition = new NetworkDeltaPosition(Vector3.zero, 0, axisToSynchronize);
                            networkState.CurrentPosition = position;
                        }
                        // Report that a log entry should be added for this update relative to the client being
                        // synchronized. The caller invokes the handler once this returns.
                        config.LogSynchronizationEntry = true;
                    }
                    flagStates.HasPositionX = config.SyncPositionX;
                    flagStates.HasPositionY = config.SyncPositionY;
                    flagStates.HasPositionZ = config.SyncPositionZ;
                    flagStates.HasPositionChange = config.SyncPositionX || config.SyncPositionY || config.SyncPositionZ;
                }
            }

            if (!config.UseQuaternionSynchronization)
            {
                if (config.SyncRotAngleX && (math.abs(NetworkTransformMath.DeltaAngle(networkState.RotAngleX, rotAngles.x)) >= rotationThreshold || flagStates.IsTeleportingNextFrame || isAxisSync || forceState))
                {
                    networkState.RotAngleX = rotAngles.x;
                    flagStates.SetHasRotation(Axis.X, true);
                    isRotationDirty = true;
                }

                if (config.SyncRotAngleY && (math.abs(NetworkTransformMath.DeltaAngle(networkState.RotAngleY, rotAngles.y)) >= rotationThreshold || flagStates.IsTeleportingNextFrame || isAxisSync || forceState))
                {
                    networkState.RotAngleY = rotAngles.y;
                    flagStates.SetHasRotation(Axis.Y, true);
                    isRotationDirty = true;
                }

                if (config.SyncRotAngleZ && (math.abs(NetworkTransformMath.DeltaAngle(networkState.RotAngleZ, rotAngles.z)) >= rotationThreshold || flagStates.IsTeleportingNextFrame || isAxisSync || forceState))
                {
                    networkState.RotAngleZ = rotAngles.z;
                    flagStates.SetHasRotation(Axis.Z, true);
                    isRotationDirty = true;
                }
            }
            else if (synchronizeRotation)
            {
                // If we are teleporting then we can skip the delta threshold check
                isRotationDirty = flagStates.IsTeleportingNextFrame || isAxisSync || forceState;
                // For quaternion synchronization, if one angle is dirty we send a full update
                if (!isRotationDirty)
                {
                    // Uses the ported conversion so this stays free of engine bindings. Verified against
                    // Quaternion.eulerAngles by NetworkTransformMathTests.
                    var previousRotation = NetworkTransformMath.EulerAngles(networkState.Rotation);
                    isRotationDirty = math.abs(NetworkTransformMath.DeltaAngle(previousRotation.x, rotAngles.x)) >= rotationThreshold
                        || math.abs(NetworkTransformMath.DeltaAngle(previousRotation.y, rotAngles.y)) >= rotationThreshold
                        || math.abs(NetworkTransformMath.DeltaAngle(previousRotation.z, rotAngles.z)) >= rotationThreshold;
                }
                if (isRotationDirty)
                {
                    networkState.Rotation = rotation;
                    flagStates.MarkChanged(AxialType.Rotation, true);
                }
            }

            // For scale, we need to check for parenting when synchronizing and/or teleporting (synchronization is always teleporting)
            if (flagStates.IsTeleportingNextFrame)
            {
                // If we are synchronizing and the associated NetworkObject has a parent then we want to send the
                // LossyScale if the NetworkObject has a parent since NetworkObject spawn order is not guaranteed
                if (flagStates.IsParented)
                {
                    networkState.LossyScale = sample.LossyScale;
                }
            }

            // Checking scale deltas when not synchronizing
            if (!isSynchronization)
            {
                if (!config.UseHalfFloatPrecision)
                {
                    if (config.SyncScaleX && (math.abs(networkState.ScaleX - scale.x) >= config.ScaleThreshold || flagStates.IsTeleportingNextFrame || isAxisSync || forceState))
                    {
                        networkState.ScaleX = scale.x;
                        flagStates.SetHasScale(Axis.X, true);
                        isScaleDirty = true;
                    }

                    if (config.SyncScaleY && (math.abs(networkState.ScaleY - scale.y) >= config.ScaleThreshold || flagStates.IsTeleportingNextFrame || isAxisSync || forceState))
                    {
                        networkState.ScaleY = scale.y;
                        flagStates.SetHasScale(Axis.Y, true);
                        isScaleDirty = true;
                    }

                    if (config.SyncScaleZ && (math.abs(networkState.ScaleZ - scale.z) >= config.ScaleThreshold || flagStates.IsTeleportingNextFrame || isAxisSync || forceState))
                    {
                        networkState.ScaleZ = scale.z;
                        flagStates.SetHasScale(Axis.Z, true);
                        isScaleDirty = true;
                    }
                }
                else if (synchronizeScale)
                {
                    var previousScale = networkState.Scale;
                    // Precompute if it is considered always dirty.
                    var alwaysDirty = flagStates.IsTeleportingNextFrame || isAxisSync || forceState;
                    // Use direct field assignment as opposed to indexing to avoid bounds checking.
                    if (alwaysDirty || math.abs(scale.x - previousScale.x) >= config.ScaleThreshold)
                    {
                        isScaleDirty = true;
                        networkState.Scale.x = scale.x;
                        flagStates.SetHasScale(Axis.X, config.SyncScaleX);
                    }

                    if (alwaysDirty || math.abs(scale.y - previousScale.y) >= config.ScaleThreshold)
                    {
                        isScaleDirty = true;
                        networkState.Scale.y = scale.y;
                        flagStates.SetHasScale(Axis.Y, config.SyncScaleY);
                    }

                    if (alwaysDirty || math.abs(scale.z - previousScale.z) >= config.ScaleThreshold)
                    {
                        isScaleDirty = true;
                        networkState.Scale.z = scale.z;
                        flagStates.SetHasScale(Axis.Z, config.SyncScaleZ);
                    }
                }
            }
            // Just apply the full local scale when synchronizing
            else if (synchronizeScale)
            {
                if (!config.UseHalfFloatPrecision)
                {
                    networkState.ScaleX = scale.x;
                    networkState.ScaleY = scale.y;
                    networkState.ScaleZ = scale.z;
                }
                else
                {
                    networkState.Scale = scale;
                }
                flagStates.MarkChanged(AxialType.Scale, true);
                isScaleDirty = true;
            }
            isDirty |= isPositionDirty || isRotationDirty || isScaleDirty;

            if (isDirty)
            {
                // Some integration/unit tests disable the NetworkTransform and there is no
                // NetworkManager
                if (config.Enabled)
                {
                    // We use the NetworkTickSystem version since ServerTime is set when updating ticks
                    networkState.NetworkTick = config.CurrentTick;
                }
            }

            // Mark the state dirty for the next network tick update to clear out the bitset values
            flagStates.IsDirty |= isDirty;

            // Apply any flag state changes
            networkState.FlagStates = flagStates;
            return isDirty;
        }
    }
}
