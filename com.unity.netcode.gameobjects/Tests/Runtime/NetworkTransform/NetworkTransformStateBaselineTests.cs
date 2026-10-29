using System;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using static Unity.Netcode.Components.NetworkTransform;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Captures the exact serialized form of a <see cref="NetworkTransformState"/> across the matrix of
    /// configurations that drive its serialization branches.
    /// </summary>
    /// <remarks>
    /// To regenerate: run this test, copy the array literal it prints on failure into
    /// <see cref="k_ExpectedSignatures"/>, and confirm every changed entry is an intended wire format change.
    /// </remarks>
    // These tests do not need to run against the Rust server.
    [IgnoreIfServiceEnvironmentVariableSet]
    internal class NetworkTransformStateBaselineTests
    {
        /// <summary>
        /// One entry per case in <see cref="BuildStateMatrix"/>, formatted as "name|byteLength|fnv1aHash".
        /// </summary>
        private static readonly string[] k_ExpectedSignatures =
        {
            "FullPrecision.AllAxes|42|B454AEF3",
            "FullPrecision.PositionOnly|18|53BC0797",
            "FullPrecision.PositionX|10|BBA3D614",
            "FullPrecision.RotationYOnly|10|3D4B5A51",
            "FullPrecision.ScaleZOnly|10|ADD3F579",
            "FullPrecision.Teleport|42|E62F1693",
            "FullPrecision.Teleport.Parented|30|0167D4EA",
            "FullPrecision.TrackByStateId|23|7109AC47",
            "FullPrecision.InLocalSpace|18|FE97B9BF",
            "QuaternionSync.Full|22|EB89F8E7",
            "QuaternionSync.Compressed|10|A86AAC6C",
            "QuaternionSync.HalfFloat|14|2629A22E",
            "QuaternionSync.Teleport|22|51DF412A",
            "HalfFloat.AllAxes|24|56C94289",
            "HalfFloat.PositionOnly|12|E0EB69C7",
            "HalfFloat.PositionXZ|10|270908C3",
            "HalfFloat.SynchronizeBase|30|83FFE5D8",
            "HalfFloat.Teleport|30|9407B934",
            "HalfFloat.Synchronizing|36|E1EC38D8",
            "HalfFloat.ScaleOnly|12|254AC316",
            "HalfFloat.EulerRotation|12|F769BA44",
            "UnreliableDeltas.FrameSync|19|81628843",
            "UnreliableDeltas.SynchronizeBaseHalfFloat|30|0DF74758",
            "UnreliableDeltas.PlainDelta|12|7B1C8307",
            "SwitchTransformSpaceWhenParented|19|DD37211C",
        };

        /// <summary>
        /// Deterministic payload values so the serialized output is stable between runs.
        /// </summary>
        private static NetworkTransformState CreateBaseState()
        {
            var state = new NetworkTransformState
            {
                NetworkTick = 12345,
                StateId = 77,
                PositionX = 1.25f,
                PositionY = -30.5f,
                PositionZ = 512.125f,
                RotAngleX = 33.75f,
                RotAngleY = 190.5f,
                RotAngleZ = 271.25f,
                // Literal components rather than Quaternion.Euler of the angles above: Euler goes through
                // native trig, which lands a ULP apart on Linux against Windows, and the uncompressed
                // quaternion cases put those bytes on the wire verbatim. (1, 2, 2, 4) / 5 is unit length and
                // has a unique largest component for the smallest three compression path.
                Rotation = new Quaternion(0.2f, 0.4f, 0.4f, 0.8f),
                ScaleX = 2.5f,
                ScaleY = 0.75f,
                ScaleZ = 4.0f,
                Scale = new Vector3(2.5f, 0.75f, 4.0f),
                LossyScale = new Vector3(5.0f, 1.5f, 8.0f),
                CurrentPosition = new Vector3(1.25f, -30.5f, 512.125f),
                DeltaPosition = new Vector3(0.125f, -0.25f, 0.5f),
            };

            var currentPosition = state.CurrentPosition;
            state.NetworkDeltaPosition = new NetworkDeltaPosition(currentPosition, state.NetworkTick, math.bool3(true));
            var deltaTarget = currentPosition + state.DeltaPosition;
            state.NetworkDeltaPosition.UpdateFrom(ref deltaTarget, state.NetworkTick);

            state.HalfVectorScale = new HalfVector3(state.Scale, math.bool3(true));
            var rotation = state.Rotation;
            state.HalfVectorRotation = new HalfVector4();
            state.HalfVectorRotation.UpdateFrom(ref rotation);
            state.HalfEulerRotation = new HalfVector3(state.RotAngleX, state.RotAngleY, state.RotAngleZ);

            return state;
        }

        private struct StateCase
        {
            public string Name;
            public NetworkTransformState State;
        }

        /// <summary>
        /// The configuration matrix. Each entry exercises a distinct path through
        /// <see cref="NetworkTransformState.NetworkSerialize{T}"/>.
        /// </summary>
        private static StateCase[] BuildStateMatrix()
        {
            var cases = new System.Collections.Generic.List<StateCase>();

            // Full precision, euler rotation, all axes.
            AddCase(cases, "FullPrecision.AllAxes", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.MarkChanged(AxialType.Rotation, true);
                f.MarkChanged(AxialType.Scale, true);
                return f;
            });

            AddCase(cases, "FullPrecision.PositionOnly", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                return f;
            });

            AddCase(cases, "FullPrecision.PositionX", f =>
            {
                f.SetHasPosition(Axis.X, true);
                return f;
            });

            AddCase(cases, "FullPrecision.RotationYOnly", f =>
            {
                f.SetHasRotation(Axis.Y, true);
                return f;
            });

            AddCase(cases, "FullPrecision.ScaleZOnly", f =>
            {
                f.SetHasScale(Axis.Z, true);
                return f;
            });

            AddCase(cases, "FullPrecision.Teleport", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.MarkChanged(AxialType.Rotation, true);
                f.MarkChanged(AxialType.Scale, true);
                f.IsTeleportingNextFrame = true;
                return f;
            });

            AddCase(cases, "FullPrecision.Teleport.Parented", f =>
            {
                f.MarkChanged(AxialType.Scale, true);
                f.IsTeleportingNextFrame = true;
                f.IsParented = true;
                return f;
            });

            AddCase(cases, "FullPrecision.TrackByStateId", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.TrackByStateId = true;
                return f;
            });

            AddCase(cases, "FullPrecision.InLocalSpace", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.InLocalSpace = true;
                return f;
            });

            // Quaternion synchronization (full precision quaternion).
            AddCase(cases, "QuaternionSync.Full", f =>
            {
                f.MarkChanged(AxialType.Rotation, true);
                f.QuaternionSync = true;
                return f;
            });

            AddCase(cases, "QuaternionSync.Compressed", f =>
            {
                f.MarkChanged(AxialType.Rotation, true);
                f.QuaternionSync = true;
                f.QuaternionCompression = true;
                return f;
            });

            AddCase(cases, "QuaternionSync.HalfFloat", f =>
            {
                f.MarkChanged(AxialType.Rotation, true);
                f.QuaternionSync = true;
                f.UseHalfFloatPrecision = true;
                return f;
            });

            AddCase(cases, "QuaternionSync.Teleport", f =>
            {
                f.MarkChanged(AxialType.Rotation, true);
                f.QuaternionSync = true;
                f.QuaternionCompression = true;
                f.IsTeleportingNextFrame = true;
                return f;
            });

            // Half float precision.
            AddCase(cases, "HalfFloat.AllAxes", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.MarkChanged(AxialType.Rotation, true);
                f.MarkChanged(AxialType.Scale, true);
                f.UseHalfFloatPrecision = true;
                return f;
            });

            AddCase(cases, "HalfFloat.PositionOnly", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.UseHalfFloatPrecision = true;
                return f;
            });

            AddCase(cases, "HalfFloat.PositionXZ", f =>
            {
                f.SetHasPosition(Axis.X, true);
                f.SetHasPosition(Axis.Z, true);
                f.UseHalfFloatPrecision = true;
                return f;
            });

            AddCase(cases, "HalfFloat.SynchronizeBase", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.UseHalfFloatPrecision = true;
                f.SynchronizeBaseHalfFloat = true;
                return f;
            });

            AddCase(cases, "HalfFloat.Teleport", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.MarkChanged(AxialType.Scale, true);
                f.UseHalfFloatPrecision = true;
                f.IsTeleportingNextFrame = true;
                return f;
            });

            AddCase(cases, "HalfFloat.Synchronizing", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.UseHalfFloatPrecision = true;
                f.IsTeleportingNextFrame = true;
                f.IsSynchronizing = true;
                return f;
            });

            AddCase(cases, "HalfFloat.ScaleOnly", f =>
            {
                f.MarkChanged(AxialType.Scale, true);
                f.UseHalfFloatPrecision = true;
                return f;
            });

            AddCase(cases, "HalfFloat.EulerRotation", f =>
            {
                f.MarkChanged(AxialType.Rotation, true);
                f.UseHalfFloatPrecision = true;
                return f;
            });

            // Delivery related flags (these only alter the bitset, but that is part of the wire format).
            AddCase(cases, "UnreliableDeltas.FrameSync", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.UseUnreliableDeltas = true;
                f.UnreliableFrameSync = true;
                return f;
            });

            // The only combination where the delivery reliability is actually derived rather than short
            // circuited: unreliable deltas enabled, not teleporting, not synchronizing, no frame sync, but the
            // half float base position is being synchronized. Every other case above has UseUnreliableDeltas
            // off, which forces reliable delivery before any of the other conditions are consulted.
            AddCase(cases, "UnreliableDeltas.SynchronizeBaseHalfFloat", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.UseUnreliableDeltas = true;
                f.UseHalfFloatPrecision = true;
                f.SynchronizeBaseHalfFloat = true;
                return f;
            });

            // The same shape with the base synchronization off, so the pair brackets the condition.
            AddCase(cases, "UnreliableDeltas.PlainDelta", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.UseUnreliableDeltas = true;
                f.UseHalfFloatPrecision = true;
                return f;
            });

            AddCase(cases, "SwitchTransformSpaceWhenParented", f =>
            {
                f.MarkChanged(AxialType.Position, true);
                f.SwitchTransformSpaceWhenParented = true;
                f.UsePositionSlerp = true;
                f.UseInterpolation = true;
                return f;
            });

            return cases.ToArray();
        }

        private static void AddCase(System.Collections.Generic.List<StateCase> cases, string name, Func<FlagStates, FlagStates> configure)
        {
            var state = CreateBaseState();
            state.FlagStates = configure(state.FlagStates);
            cases.Add(new StateCase { Name = name, State = state });
        }

        /// <summary>
        /// FNV-1a over the serialized payload. Small, stable, and dependency free.
        /// </summary>
        private static uint Fnv1a(byte[] bytes)
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;
            var hash = offsetBasis;
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= prime;
            }
            return hash;
        }

        private static byte[] Serialize(NetworkTransformState state)
        {
            // Resolving the delivery reliability used to happen inside NetworkSerialize. It now happens before
            // writing, because the batched synchronization mode uses the result to pick which of its two per
            // tick messages a state belongs to. Every send path calls this first, so the baseline does too;
            // without it these signatures would move for a reason that has nothing to do with the wire format.
            state.UpdateReliability();

            var writer = new FastBufferWriter(1024, Allocator.Temp);
            try
            {
                writer.WriteNetworkSerializable(state);
                return writer.ToArray();
            }
            finally
            {
                writer.Dispose();
            }
        }

        /// <summary>
        /// Verifies the serialized form of every configuration in the matrix still matches the recorded baseline.
        /// </summary>
        [Test]
        public void NetworkTransformStateSerializationBaseline()
        {
            var cases = BuildStateMatrix();
            var actual = new string[cases.Length];

            for (int i = 0; i < cases.Length; i++)
            {
                var bytes = Serialize(cases[i].State);
                actual[i] = $"{cases[i].Name}|{bytes.Length}|{Fnv1a(bytes):X8}";
            }

            if (k_ExpectedSignatures.Length != cases.Length)
            {
                Assert.Fail($"No baseline recorded (expected {cases.Length} entries, found {k_ExpectedSignatures.Length}). " +
                    $"Verify this is a new or intentionally changed wire format, then paste the following into {nameof(k_ExpectedSignatures)}:\n\n{FormatLiteral(actual)}");
            }

            var mismatches = new StringBuilder();
            for (int i = 0; i < cases.Length; i++)
            {
                if (k_ExpectedSignatures[i] != actual[i])
                {
                    mismatches.AppendLine($"  [{i}] expected \"{k_ExpectedSignatures[i]}\" but was \"{actual[i]}\"");
                }
            }

            if (mismatches.Length > 0)
            {
                Assert.Fail($"The serialized {nameof(NetworkTransformState)} no longer matches the recorded baseline. " +
                    $"If this is an intentional wire format change, update {nameof(k_ExpectedSignatures)}.\n{mismatches}\nUpdated baseline:\n\n{FormatLiteral(actual)}");
            }
        }

        /// <summary>
        /// Verifies every configuration in the matrix survives a write and read back.
        /// </summary>
        /// <remarks>
        /// The baseline proves the bytes did not change. This proves they still round trip, so a baseline
        /// regenerated against a broken serializer is not silently accepted.
        /// </remarks>
        [Test]
        public void NetworkTransformStateSerializationRoundTrip()
        {
            foreach (var stateCase in BuildStateMatrix())
            {
                var bytes = Serialize(stateCase.State);
                NetworkTransformState deserialized;
                var reader = new FastBufferReader(bytes, Allocator.Temp);
                try
                {
                    reader.ReadNetworkSerializable(out deserialized);
                    Assert.AreEqual(bytes.Length, reader.Position,
                        $"[{stateCase.Name}] Reader consumed {reader.Position} of {bytes.Length} bytes!");
                }
                finally
                {
                    reader.Dispose();
                }

                Assert.AreEqual(stateCase.State.NetworkTick, deserialized.NetworkTick,
                    $"[{stateCase.Name}] NetworkTick did not survive the round trip!");

                if (stateCase.State.FlagStates.TrackByStateId)
                {
                    Assert.AreEqual(stateCase.State.StateId, deserialized.StateId,
                        $"[{stateCase.Name}] StateId did not survive the round trip!");
                }

                AssertFlagsSurvived(stateCase, deserialized);

                // Re-serializing what was read back must reproduce the original payload byte for byte.
                // This is the primary round trip assertion because it covers every field without the test
                // needing to know which of them the serializer derives on the way out.
                var reserialized = Serialize(deserialized);
                Assert.AreEqual(bytes.Length, reserialized.Length,
                    $"[{stateCase.Name}] Re-serialized payload was {reserialized.Length} bytes but the original was {bytes.Length}!");
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (bytes[i] != reserialized[i])
                    {
                        Assert.Fail($"[{stateCase.Name}] Re-serialized payload differs at byte {i}: expected 0x{bytes[i]:X2} but was 0x{reserialized[i]:X2}!");
                    }
                }
            }
        }

        /// <summary>
        /// Compares every flag that a state update carries, with the exception of
        /// <see cref="FlagStates.ReliableSequenced"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="NetworkTransformState.UpdateReliability"/> derives ReliableSequenced, and
        /// <see cref="Serialize"/> takes the state by value and calls it on that copy. The flag reaches the
        /// bytes and the state read back from them, but never the source state the case holds.
        /// </remarks>
        private static void AssertFlagsSurvived(StateCase stateCase, NetworkTransformState deserialized)
        {
            var expected = stateCase.State.FlagStates;
            var actual = deserialized.FlagStates;

            void Check(string flag, bool expectedValue, bool actualValue)
            {
                Assert.AreEqual(expectedValue, actualValue, $"[{stateCase.Name}] Flag {flag} did not survive the round trip!");
            }

            Check(nameof(FlagStates.InLocalSpace), expected.InLocalSpace, actual.InLocalSpace);
            Check(nameof(FlagStates.HasPositionX), expected.HasPositionX, actual.HasPositionX);
            Check(nameof(FlagStates.HasPositionY), expected.HasPositionY, actual.HasPositionY);
            Check(nameof(FlagStates.HasPositionZ), expected.HasPositionZ, actual.HasPositionZ);
            Check(nameof(FlagStates.HasRotAngleX), expected.HasRotAngleX, actual.HasRotAngleX);
            Check(nameof(FlagStates.HasRotAngleY), expected.HasRotAngleY, actual.HasRotAngleY);
            Check(nameof(FlagStates.HasRotAngleZ), expected.HasRotAngleZ, actual.HasRotAngleZ);
            Check(nameof(FlagStates.HasScaleX), expected.HasScaleX, actual.HasScaleX);
            Check(nameof(FlagStates.HasScaleY), expected.HasScaleY, actual.HasScaleY);
            Check(nameof(FlagStates.HasScaleZ), expected.HasScaleZ, actual.HasScaleZ);
            Check(nameof(FlagStates.IsTeleportingNextFrame), expected.IsTeleportingNextFrame, actual.IsTeleportingNextFrame);
            Check(nameof(FlagStates.UseInterpolation), expected.UseInterpolation, actual.UseInterpolation);
            Check(nameof(FlagStates.QuaternionSync), expected.QuaternionSync, actual.QuaternionSync);
            Check(nameof(FlagStates.QuaternionCompression), expected.QuaternionCompression, actual.QuaternionCompression);
            Check(nameof(FlagStates.UseHalfFloatPrecision), expected.UseHalfFloatPrecision, actual.UseHalfFloatPrecision);
            Check(nameof(FlagStates.IsSynchronizing), expected.IsSynchronizing, actual.IsSynchronizing);
            Check(nameof(FlagStates.UsePositionSlerp), expected.UsePositionSlerp, actual.UsePositionSlerp);
            Check(nameof(FlagStates.IsParented), expected.IsParented, actual.IsParented);
            Check(nameof(FlagStates.SynchronizeBaseHalfFloat), expected.SynchronizeBaseHalfFloat, actual.SynchronizeBaseHalfFloat);
            Check(nameof(FlagStates.UseUnreliableDeltas), expected.UseUnreliableDeltas, actual.UseUnreliableDeltas);
            Check(nameof(FlagStates.UnreliableFrameSync), expected.UnreliableFrameSync, actual.UnreliableFrameSync);
            Check(nameof(FlagStates.SwitchTransformSpaceWhenParented), expected.SwitchTransformSpaceWhenParented, actual.SwitchTransformSpaceWhenParented);
            Check(nameof(FlagStates.TrackByStateId), expected.TrackByStateId, actual.TrackByStateId);
        }

        private static string FormatLiteral(string[] signatures)
        {
            var builder = new StringBuilder();
            builder.AppendLine("        private static readonly string[] k_ExpectedSignatures =");
            builder.AppendLine("        {");
            foreach (var signature in signatures)
            {
                builder.AppendLine($"            \"{signature}\",");
            }
            builder.AppendLine("        };");
            return builder.ToString();
        }
    }
}
