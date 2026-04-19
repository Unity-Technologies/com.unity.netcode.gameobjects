using Unity.Mathematics;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// The transform's grid state representation that is used for:
    /// - Synchronizing transforms changes in states.
    /// - Grid-space positioning and processing.
    /// </summary>
    internal struct TransformGridState : ITransformState<TransformGridState>
    {
        public float Precision;
        public float InvPrecision;
        public bool DirtyScale;
        public bool DirtyPosition;
        public bool DirtyRotation;

        public int Header_Size;
        public int Payload_Size;
        public byte DirtyFlags { get; private set; }

        public Vector3HalfState Scale;
        public Vector3State Position;
        public ForwardVector Forward;
        public QuaternionState Rotation;

        public Vector3 ScaleFloat;
        public Vector3 PositionFloat;
        public int Index;
        public ushort TransformIdentifier;

        internal ushort PreviousTransformIdentifier;

        public Vector3 LastPositionUpdate;
        public Vector3 CurrentScale;

        public void ApplyState(TransformGridState state)
        {
            if (TransformIdentifier != state.TransformIdentifier)
            {
                Debug.Log($"MISMATCH CONFLICT IN STATE PROCESSING! Applying TID: {state.TransformIdentifier} to previous state for TID: {TransformIdentifier}!");
            }
            Index = state.Index;
            Precision = state.Precision;
            InvPrecision = state.InvPrecision;
            DirtyScale = state.DirtyScale;
            DirtyPosition = state.DirtyPosition;
            DirtyRotation = state.DirtyRotation;
            Scale.ApplyState(state.Scale);
            Position.ApplyState(state.Position);
            Rotation.ApplyState(state.Rotation);
        }

        public bool HasDelta()
        {
            // Currently disabling scale updates until Scale is converted back to
            // use the Vector3State.
            //return Scale.HasDelta() || Position.HasDelta() || Rotation.HasDelta();
            return Position.HasDelta() || Rotation.HasDelta();
        }

        public void Clear()
        {
            DirtyScale = false;
            DirtyPosition = false;
            DirtyRotation = false;
            InvPrecision = 0.0f;
            Precision = 0.0f;
            ScaleFloat = Vector3.zero;
            PositionFloat = Vector3.zero;
            Index = 0;
            Scale.Clear();
            Position.Clear();
            Rotation.Clear();
        }

        public void Initialize()
        {
            Scale.Initialize();
            Position.Initialize();
            Rotation.Initialize();
        }

        public void Dispose()
        {
            Scale.Dispose();
            Position.Dispose();
            Rotation.Dispose();
        }

        /// <summary>
        /// TODO: We may or may not need this.
        /// (Currently nothing uses this method when writing this)
        /// </summary>
        public void Compress()
        {
            if (Scale.HasDelta())
            {
                Scale.Compress();
            }
            if (Position.HasDelta())
            {
                Position.Compress();
            }
            if (Rotation.HasDelta())
            {
                Rotation.Compress();
            }
        }

        public void Decompress()
        {
            if (DirtyScale)
            {
                Scale.InvPrecision = InvPrecision;
                ScaleFloat = Scale.UpdateFromValue(CurrentScale);
            }

            if (DirtyPosition)
            {
                // Convert the state update to a Vector3
                var update = Position.ToVector3(InvPrecision);

                // Get our position relative to the last state update
                // and not our current position.
                PositionFloat = LastPositionUpdate;
                for (int i = 0; i < 3; i++)
                {
                    // Only update axis with something other than 0.0f.
                    if (update[i] != 0.0f)
                    {
                        PositionFloat[i] = update[i];
                    }
                }
            }

            if (DirtyRotation)
            {
                Rotation.Decompress();
            }
        }

        public unsafe (byte, int, int) DebugWriteState(FastBufferWriter writer, ushort previousTranaformIdentifier)
        {
            PreviousTransformIdentifier = previousTranaformIdentifier;
            WriteState(writer);
            return (DirtyFlags, Header_Size, Payload_Size);
        }

        public void WriteState(FastBufferWriter writer)
        {
            var startPosition = writer.Position;
            // Combine the transform identifier (ushort) and the
            // axis types flags together which will keep the header
            // size at 2 bytes per transform instance per state update.
            // Note:
            // For additional configurations, we can use the upper bits
            // of the transform information header to signal there is
            // additional information being provided (i.e. synchronize, teleport, etc.).
            // Optimization on identifier size:
            // Just send the delta between identifiers. Under scenarios where the delta
            // is less than 16 then the total transform header size is 1 byte. For everything
            // between 16 and 4096 (very unlikely) the header size will be 2 bytes. Anything
            // beyond that number it becomes a 3 byte header per state update.
            // Note:
            // This could be handled by breaking updates into area of interest and organizing
            // transforms by their grid node. Then, depending upon the number of spawned instances
            // we would send further nodes at a lesser tick frequency that is interleaved between
            // network ticks.
            var transformInfo = (uint)(TransformIdentifier - PreviousTransformIdentifier);

            // The lower 3 bits are reserved for axis type flags.
            transformInfo = transformInfo << 3;
            transformInfo |= (uint)(Position.HasDelta() ? 1 : 0);
            transformInfo |= (uint)(Rotation.HasDelta() ? 2 : 0);
            transformInfo |= (uint)(Scale.HasDelta() ? 4 : 0);
            BytePacker.WriteValueBitPacked(writer, transformInfo);

            // Tacking the size of the written header (keeping this for tracking and future purposes)
            Header_Size = writer.Position - startPosition;
            startPosition = writer.Position;

            // Write any axis type that has a delta
            if (Position.HasDelta())
            {
                Position.WriteState(writer);
            }

            if (Rotation.HasDelta())
            {
                Rotation.WriteState(writer);
            }

            if (Scale.HasDelta())
            {
                Scale.WriteState(writer);
            }
            // Tacking the size of the written payload (keeping this for tracking and future purposes)
            Payload_Size = writer.Position - startPosition;

            // Set the local dirty flags from the transform info
            // (This is more for current debugging purposes.)
            DirtyFlags = (byte)(transformInfo & 0b111);
        }

        public unsafe void ReadStateWithPrevious(FastBufferReader reader, ushort previousIdentifier)
        {
            PreviousTransformIdentifier = previousIdentifier;
            ReadState(reader);
        }

        public unsafe void ReadState(FastBufferReader reader)
        {
            var dirtyFlags = (byte)0;
            var startPosition = reader.Position;
            var transformInfo = (uint)0;

            // Read the transform header and extract the transform identifier
            // and the axis types that have state updates.
            ByteUnpacker.ReadValuePacked(reader, out transformInfo);

            // Get the dirty flags
            dirtyFlags = (byte)(transformInfo & 0b111);
            transformInfo |= (uint)(Position.HasDelta() ? 1 : 0);
            transformInfo |= (uint)(Rotation.HasDelta() ? 2 : 0);
            transformInfo |= (uint)(Scale.HasDelta() ? 4 : 0);
            DirtyPosition = (transformInfo & 1) == 1;
            DirtyRotation = (transformInfo & 2) == 2;
            DirtyScale = (transformInfo & 4) == 4;

            // Get the transform identifier delta from the previous one
            TransformIdentifier = ((ushort)(transformInfo >> 3));
            // Add the previous identifier value to the delta
            TransformIdentifier += PreviousTransformIdentifier;

            // Tacking the size of the read header (keeping this for tracking and future purposes)
            Header_Size = reader.Position - startPosition;
            startPosition = reader.Position;

            // Read state updates based on dirty flags
            if (DirtyPosition)
            {
                DirtyPosition = true;
                Position.ReadState(reader);
            }

            if (DirtyRotation)
            {
                DirtyRotation = true;
                Rotation.ReadState(reader);
                Rotation.Decompress();
            }

            if (DirtyScale)
            {
                Scale.ReadState(reader);
                ScaleFloat = math.float3(Scale.Axis);
            }

            // Experimental
            //if ((dirtyFlags & 0x04) == 0x04)
            //{
            //    DirtyRotation = true;
            //    Forward.ReadState(reader);
            //}

            // Tacking the size of the read payload (keeping this for tracking and future purposes)
            Payload_Size = reader.Position - startPosition;

            // Set the local dirty flags from the transform info
            // (This is more for current debugging purposes.)
            DirtyFlags = dirtyFlags;
        }
    }

    internal interface ITransformStateComponent<T> : ITransformState<T>
    {
        public unsafe void Compress();

        public unsafe void Decompress();
    }
}
