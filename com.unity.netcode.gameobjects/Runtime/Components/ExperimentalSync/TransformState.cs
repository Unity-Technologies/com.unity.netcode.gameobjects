using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Unity.Netcode
{
    /// <summary>
    /// Used for authotitative transform processing to detect
    /// changes in state while also optimizing bandwidth consumption.
    /// </summary>
    [BurstCompile]
    internal struct TransformState : IDisposable
    {
        public TransformGridState GridStatePrevious;
        public TransformGridState GridStateCurrent;
        public TransformGridState GridStateDelta;

        public ulong EntityIdentifier;
        public bool IsFirstSync;

        /// <summary>
        /// Initializes the 3 state (current, previous, and the delta between the two)
        /// </summary>
        public void Initialize()
        {
            GridStatePrevious = new TransformGridState();
            GridStatePrevious.Initialize();
            GridStateCurrent = new TransformGridState();
            GridStateCurrent.Initialize();
            GridStateDelta = new TransformGridState();
            GridStateDelta.Initialize();
            IsFirstSync = true;
        }

        public void Dispose()
        {
            GridStatePrevious.Dispose();
            GridStateCurrent.Dispose();
            GridStateDelta.Dispose();
        }

        public void UpdateIds(TransformStateSync transformStateSync)
        {
            // We may not need the EntityIdentifier
            EntityIdentifier = EntityId.ToULong(transformStateSync.GetEntityId());
            GridStateDelta.TransformIdentifier = GridStatePrevious.TransformIdentifier = GridStateCurrent.TransformIdentifier = transformStateSync.TransformIdentifier;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ProcessCurrentState(int index, TransformAccess transformAccess, int precision, bool isFullSynch)
        {
            if (transformAccess.isValid)
            {
                // Get and set the current transform state
                GridStateCurrent.Index = index;

                // TODO: Replace with Vector3State
                GridStateCurrent.Scale.X = (int)(transformAccess.localScale.x * precision);
                GridStateCurrent.Scale.Y = (int)(transformAccess.localScale.y * precision);
                GridStateCurrent.Scale.Z = (int)(transformAccess.localScale.z * precision);

                GridStateCurrent.Position.ApplyCurrent(transformAccess.position, precision);

                // Experimental forward vector synchronization
                //var forward = transformAccess.rotation.normalized * Vector3.forward;
                //GridStateCurrent.Forward.X = (int)(forward.x * precision);
                //GridStateCurrent.Forward.Y = (int)(forward.y * precision);
                //GridStateCurrent.Forward.Z = (int)(forward.z * precision);
                //GridStateCurrent.Forward.Forward = forward;

                GridStateCurrent.Rotation.X = (int)(transformAccess.rotation.x * precision);
                GridStateCurrent.Rotation.Y = (int)(transformAccess.rotation.y * precision);
                GridStateCurrent.Rotation.Z = (int)(transformAccess.rotation.z * precision);
                GridStateCurrent.Rotation.W = (int)(transformAccess.rotation.w * precision);
                GridStateCurrent.Rotation.Rotation = transformAccess.rotation;

                // Calculate the delta between the previous and current states.
                GridStateDelta.Index = index;
                GridStateDelta.Scale.X = GridStateCurrent.Scale.X - GridStatePrevious.Scale.X;
                GridStateDelta.Scale.Y = GridStateCurrent.Scale.Y - GridStatePrevious.Scale.Y;
                GridStateDelta.Scale.Z = GridStateCurrent.Scale.Z - GridStatePrevious.Scale.Z;


                GridStateDelta.Position.ToDelta(GridStateCurrent.Position, GridStatePrevious.Position, isFullSynch || IsFirstSync);

                // Experimental forward vector synchronization
                //GridStateDelta.Forward.X = GridStateCurrent.Forward.X - GridStatePrevious.Forward.X;
                //GridStateDelta.Forward.Y = GridStateCurrent.Forward.Y - GridStatePrevious.Forward.Y;
                //GridStateDelta.Forward.Z = GridStateCurrent.Forward.Z - GridStatePrevious.Forward.Z;

                GridStateDelta.Rotation.X = GridStateCurrent.Rotation.X - GridStatePrevious.Rotation.X;
                GridStateDelta.Rotation.Y = GridStateCurrent.Rotation.Y - GridStatePrevious.Rotation.Y;
                GridStateDelta.Rotation.Z = GridStateCurrent.Rotation.Z - GridStatePrevious.Rotation.Z;
                GridStateDelta.Rotation.W = GridStateCurrent.Rotation.W - GridStatePrevious.Rotation.W;

                // Check for and record deltas between the current and previous states
                GridStateDelta.DirtyScale = false;
                if (GridStateDelta.Scale.HasDelta())
                {
                    GridStateDelta.DirtyScale = true;
                    GridStateDelta.Scale.Axis = new half3(transformAccess.localScale);
                    // TODO: this could be removed
                    GridStateDelta.Scale.InvPrecision = 1.0f / precision;
                }

                GridStateDelta.DirtyPosition = false;
                if (GridStateDelta.Position.HasDelta())
                {
                    GridStateDelta.DirtyPosition = true;
                    GridStateDelta.Position.Compress();
                    //GridStateDelta.Position.Axis = new half3(transformAccess.position);

                    // TODO: this could be removed
                    //GridStateDelta.Position.InvPrecision = 1.0f / precision;
                    IsFirstSync = false;
                }

                GridStateDelta.DirtyRotation = false;

                // Experimental forward vector synchronization
                //if (GridStateDelta.Forward.HasDelta())
                //{
                //    GridStateDelta.DirtyRotation = true;
                //    GridStateDelta.Forward.ApplyState(GridStateCurrent.Forward);
                //}

                GridStateDelta.Rotation.IsDirty = false;
                if (GridStateDelta.Rotation.HasDelta())
                {
                    GridStateDelta.DirtyRotation = true;
                    GridStateDelta.Rotation.IsDirty = true;
                    GridStateDelta.Rotation.ApplyState(GridStateCurrent.Rotation);
                    GridStateDelta.Rotation.Compress();
                }

                // Apply the state even if there were no deltas so we 
                GridStatePrevious.ApplyState(GridStateCurrent);
            }

            return GridStateDelta.HasDelta();
        }
    }

    /// <summary>
    /// Any transform axis type handler has to implement this.
    /// </summary>
    /// <typeparam name="T">The state type being synchronized.</typeparam>
    internal interface ITransformState<T> : IDisposable
    {
        public void ApplyState(T state);

        public bool HasDelta();

        public void Initialize();

        public unsafe void WriteState(FastBufferWriter writer);

        public unsafe void ReadState(FastBufferReader reader);
    }

}
