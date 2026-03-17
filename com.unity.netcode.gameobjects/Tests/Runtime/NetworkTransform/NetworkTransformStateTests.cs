using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using static Unity.Netcode.Components.NetworkTransform;
using Object = UnityEngine.Object;


namespace Unity.Netcode.RuntimeTests
{
    // These tests do not need to run against the Rust server.
    [IgnoreIfServiceEnvironmentVariableSet]
    internal class NetworkTransformStateTests
    {
        [Test]
        public void NetworkTransformStateFlags()
        {
            // The current number of flags on the NetworkTransformState
            var numFlags = 24;

            var indexValues = new uint[numFlags];

            var currentFlag = (uint)0x00000001;
            for (int j = 0; j < numFlags - 1; j++)
            {
                indexValues[j] = currentFlag;
                currentFlag = currentFlag << 1;
            }

            // TrackByStateId is unique
            indexValues[numFlags - 1] = 0x10000000;

            var boolSet = new bool[numFlags];

            InlinedBitmathSerialization(ref numFlags, ref indexValues, ref boolSet);
        }


        private void InlinedBitmathSerialization(ref int numFlags, ref uint[] indexValues, ref bool[] boolSet)
        {
            NetworkTransformState transformState;
            FastBufferWriter writer;
            FastBufferReader reader;
            // Test setting one at a time.
            for (int j = 0; j < numFlags; j++)
            {
                // reset previous test if needed
                if (j > 0)
                {
                    boolSet[j - 1] = false;
                }

                boolSet[j] = true;

                transformState = new NetworkTransformState()
                {
                    FlagStates = new FlagStates()
                    {
                        InLocalSpace = boolSet[0],
                        HasPositionX = boolSet[1],
                        HasPositionY = boolSet[2],
                        HasPositionZ = boolSet[3],
                        HasRotAngleX = boolSet[4],
                        HasRotAngleY = boolSet[5],
                        HasRotAngleZ = boolSet[6],
                        HasScaleX = boolSet[7],
                        HasScaleY = boolSet[8],
                        HasScaleZ = boolSet[9],
                        IsTeleportingNextFrame = boolSet[10],
                        UseInterpolation = boolSet[11],
                        QuaternionSync = boolSet[12],
                        QuaternionCompression = boolSet[13],
                        UseHalfFloatPrecision = boolSet[14],
                        IsSynchronizing = boolSet[15],
                        UsePositionSlerp = boolSet[16],
                        IsParented = boolSet[17],
                        SynchronizeBaseHalfFloat = boolSet[18],
                        ReliableSequenced = boolSet[19],
                        UseUnreliableDeltas = boolSet[20],
                        UnreliableFrameSync = boolSet[21],
                        SwitchTransformSpaceWhenParented = boolSet[22],
                        TrackByStateId = boolSet[23],
                    },
                };

                writer = new FastBufferWriter(64, Allocator.Temp);
                BytePacker.WriteValueBitPacked(writer, transformState.FlagStates.GetBitsetRepresentation());

                // Test the bitset representation of the serialization matches the pre-refactor serialization
                reader = new FastBufferReader(writer, Allocator.None);
                ByteUnpacker.ReadValueBitPacked(reader, out uint serializedBitset);

                Assert.True((serializedBitset & indexValues[j]) == indexValues[j], $"[FlagTest][Individual] Set flag value {indexValues[j]} at index {j}, but BitSet value did not match!");

                // reset the reader to the beginning of the buffer
                reader.Seek(0);

                ByteUnpacker.ReadValueBitPacked(reader, out uint bitFlags);
                // Test the deserialized values match the original values
                var deserialized = new NetworkTransformState();
                // Set the flags
                deserialized.FlagStates.SetStateFromBitset(bitFlags);

                AssertTransformStateEquals(boolSet, deserialized, "Flag serialization");
            }

            // Test setting all flag values
            transformState = new NetworkTransformState()
            {
                FlagStates = new FlagStates()
                {
                    InLocalSpace = true,
                    HasPositionX = true,
                    HasPositionY = true,
                    HasPositionZ = true,
                    HasRotAngleX = true,
                    HasRotAngleY = true,
                    HasRotAngleZ = true,
                    HasScaleX = true,
                    HasScaleY = true,
                    HasScaleZ = true,
                    IsTeleportingNextFrame = true,
                    UseInterpolation = true,
                    QuaternionSync = true,
                    QuaternionCompression = true,
                    UseHalfFloatPrecision = true,
                    IsSynchronizing = true,
                    UsePositionSlerp = true,
                    IsParented = true,
                    SynchronizeBaseHalfFloat = true,
                    ReliableSequenced = true,
                    UseUnreliableDeltas = true,
                    UnreliableFrameSync = true,
                    SwitchTransformSpaceWhenParented = true,
                    TrackByStateId = true,
                },
            };

            writer = new FastBufferWriter(64, Allocator.Temp);
            BytePacker.WriteValueBitPacked(writer, transformState.FlagStates.GetBitsetRepresentation());

            var serializedBuffer = writer.ToArray();

            // Use a uint to set all bits to true in a legacy style bitset
            uint bitset = 0;
            for (int i = 0; i < numFlags; i++)
            {
                bitset |= indexValues[i];
            }

            var legacyBitsetWriter = new FastBufferWriter(64, Allocator.Temp);
            BytePacker.WriteValueBitPacked(legacyBitsetWriter, bitset);

            // Test refactored serialization matches pre-refactor flag serialization
            Assert.AreEqual(legacyBitsetWriter.ToArray(), serializedBuffer, "[Flag serialization] Serialized NetworkTransformState doesn't match original serialization!");

            reader = new FastBufferReader(legacyBitsetWriter, Allocator.None);
            ByteUnpacker.ReadValueBitPacked(reader, out uint bitFlagsState);
            // Test the deserialized values match the original values
            var deserializedState = new NetworkTransformState();
            // Set the flags
            deserializedState.FlagStates.SetStateFromBitset(bitFlagsState);

            Array.Fill(boolSet, true);
            AssertTransformStateEquals(boolSet, deserializedState, "Read bitset");
        }

        private void AssertTransformStateEquals(bool[] expected, NetworkTransformState actual, string testName)
        {
            Assert.AreEqual(expected[0], actual.FlagStates.InLocalSpace, $"{testName} Flag {nameof(FlagStates.InLocalSpace)} is incorrect!");
            Assert.AreEqual(expected[1], actual.FlagStates.HasPositionX, $"{testName} Flag {nameof(FlagStates.HasPositionX)} is incorrect!");
            Assert.AreEqual(expected[2], actual.FlagStates.HasPositionY, $"{testName} Flag {nameof(FlagStates.HasPositionY)} is incorrect!");
            Assert.AreEqual(expected[3], actual.FlagStates.HasPositionZ, $"{testName} Flag {nameof(FlagStates.HasPositionZ)} is incorrect!");
            Assert.AreEqual(expected[4], actual.FlagStates.HasRotAngleX, $"{testName} Flag {nameof(FlagStates.HasRotAngleX)} is incorrect!");
            Assert.AreEqual(expected[5], actual.FlagStates.HasRotAngleY, $"{testName} Flag {nameof(FlagStates.HasRotAngleY)} is incorrect!");
            Assert.AreEqual(expected[6], actual.FlagStates.HasRotAngleZ, $"{testName} Flag {nameof(FlagStates.HasRotAngleZ)} is incorrect!");
            Assert.AreEqual(expected[7], actual.FlagStates.HasScaleX, $"{testName} Flag {nameof(FlagStates.HasScaleX)} is incorrect!");
            Assert.AreEqual(expected[8], actual.FlagStates.HasScaleY, $"{testName} Flag {nameof(FlagStates.HasScaleY)} is incorrect!");
            Assert.AreEqual(expected[9], actual.FlagStates.HasScaleZ, $"{testName} Flag {nameof(FlagStates.HasScaleZ)} is incorrect!");
            Assert.AreEqual(expected[10], actual.FlagStates.IsTeleportingNextFrame, $"{testName} Flag {nameof(FlagStates.IsTeleportingNextFrame)} is incorrect!");
            Assert.AreEqual(expected[11], actual.FlagStates.UseInterpolation, $"{testName} Flag {nameof(FlagStates.UseInterpolation)} is incorrect!");
            Assert.AreEqual(expected[12], actual.FlagStates.QuaternionSync, $"{testName} Flag {nameof(FlagStates.QuaternionSync)} is incorrect!");
            Assert.AreEqual(expected[13], actual.FlagStates.QuaternionCompression, $"{testName} Flag {nameof(FlagStates.QuaternionCompression)} is incorrect!");
            Assert.AreEqual(expected[14], actual.FlagStates.UseHalfFloatPrecision, $"{testName} Flag {nameof(FlagStates.UseHalfFloatPrecision)} is incorrect!");
            Assert.AreEqual(expected[15], actual.FlagStates.IsSynchronizing, $"{testName} Flag {nameof(FlagStates.IsSynchronizing)} is incorrect!");
            Assert.AreEqual(expected[16], actual.FlagStates.UsePositionSlerp, $"{testName} Flag {nameof(FlagStates.UsePositionSlerp)} is incorrect!");
            Assert.AreEqual(expected[17], actual.FlagStates.IsParented, $"{testName} Flag {nameof(FlagStates.IsParented)} is incorrect!");
            Assert.AreEqual(expected[18], actual.FlagStates.SynchronizeBaseHalfFloat, $"{testName} Flag {nameof(FlagStates.SynchronizeBaseHalfFloat)} is incorrect!");
            Assert.AreEqual(expected[19], actual.FlagStates.ReliableSequenced, $"{testName} Flag {nameof(FlagStates.ReliableSequenced)} is incorrect!");
            Assert.AreEqual(expected[20], actual.FlagStates.UseUnreliableDeltas, $"{testName} Flag {nameof(FlagStates.UseUnreliableDeltas)} is incorrect!");
            Assert.AreEqual(expected[21], actual.FlagStates.UnreliableFrameSync, $"{testName} Flag {nameof(FlagStates.UnreliableFrameSync)} is incorrect!");
            Assert.AreEqual(expected[22], actual.FlagStates.SwitchTransformSpaceWhenParented, $"{testName} Flag {nameof(FlagStates.SwitchTransformSpaceWhenParented)} is incorrect!");
            Assert.AreEqual(expected[23], actual.FlagStates.TrackByStateId, $"{testName} Flag {nameof(FlagStates.TrackByStateId)} is incorrect!");
        }

    }

    // These tests do not need to run against the Rust server.
    [IgnoreIfServiceEnvironmentVariableSet]
    [TestFixture(TransformSpace.World, Precision.Full, Rotation.Euler)]
    [TestFixture(TransformSpace.World, Precision.Half, Rotation.Euler)]
    [TestFixture(TransformSpace.Local, Precision.Full, Rotation.Euler)]
    [TestFixture(TransformSpace.Local, Precision.Half, Rotation.Euler)]
    [TestFixture(TransformSpace.World, Precision.Full, Rotation.Quaternion)]
    [TestFixture(TransformSpace.World, Precision.Half, Rotation.Quaternion)]
    [TestFixture(TransformSpace.Local, Precision.Full, Rotation.Quaternion)]
    [TestFixture(TransformSpace.Local, Precision.Half, Rotation.Quaternion)]
    internal class NetworkTransformStateConfigurationTests
    {
        public enum SyncAxis
        {
            SyncPosX,
            SyncPosY,
            SyncPosZ,
            SyncPosXY,
            SyncPosXZ,
            SyncPosYZ,
            SyncPosXYZ,
            SyncRotX,
            SyncRotY,
            SyncRotZ,
            SyncRotXY,
            SyncRotXZ,
            SyncRotYZ,
            SyncRotXYZ,
            SyncScaleX,
            SyncScaleY,
            SyncScaleZ,
            SyncScaleXY,
            SyncScaleXZ,
            SyncScaleYZ,
            SyncScaleXYZ,
            SyncAllX,
            SyncAllY,
            SyncAllZ,
            SyncAllXY,
            SyncAllXZ,
            SyncAllYZ,
            SyncAllXYZ
        }

        public enum TransformSpace
        {
            World,
            Local
        }

        public enum Rotation
        {
            Euler,
            Quaternion
        }

        public enum SynchronizationType
        {
            Delta,
            Teleport
        }

        public enum Precision
        {
            Half,
            Full
        }

        private TransformSpace m_TransformSpace;
        private Precision m_Precision;
        private Rotation m_Rotation;

        public NetworkTransformStateConfigurationTests(TransformSpace transformSpace, Precision precision, Rotation rotation)
        {
            m_TransformSpace = transformSpace;
            m_Precision = precision;
            m_Rotation = rotation;
        }

        private bool WillAnAxisBeSynchronized(ref NetworkTransform networkTransform)
        {
            return networkTransform.SyncScaleX || networkTransform.SyncScaleY || networkTransform.SyncScaleZ ||
                networkTransform.SyncRotAngleX || networkTransform.SyncRotAngleY || networkTransform.SyncRotAngleZ ||
                networkTransform.SyncPositionX || networkTransform.SyncPositionY || networkTransform.SyncPositionZ;
        }

        [Test]
        public void TestSyncAxes([Values] SynchronizationType synchronizationType, [Values] SyncAxis syncAxis)

        {
            bool inLocalSpace = m_TransformSpace == TransformSpace.Local;
            bool isTeleporting = synchronizationType == SynchronizationType.Teleport;
            bool syncPosX = syncAxis == SyncAxis.SyncPosX || syncAxis == SyncAxis.SyncPosXY || syncAxis == SyncAxis.SyncPosXZ || syncAxis == SyncAxis.SyncPosXYZ || syncAxis == SyncAxis.SyncAllX || syncAxis == SyncAxis.SyncAllXY || syncAxis == SyncAxis.SyncAllXZ || syncAxis == SyncAxis.SyncAllXYZ;
            bool syncPosY = syncAxis == SyncAxis.SyncPosY || syncAxis == SyncAxis.SyncPosXY || syncAxis == SyncAxis.SyncPosYZ || syncAxis == SyncAxis.SyncPosXYZ || syncAxis == SyncAxis.SyncAllY || syncAxis == SyncAxis.SyncAllXY || syncAxis == SyncAxis.SyncAllYZ || syncAxis == SyncAxis.SyncAllXYZ;
            bool syncPosZ = syncAxis == SyncAxis.SyncPosZ || syncAxis == SyncAxis.SyncPosXZ || syncAxis == SyncAxis.SyncPosYZ || syncAxis == SyncAxis.SyncPosXYZ || syncAxis == SyncAxis.SyncAllZ || syncAxis == SyncAxis.SyncAllXZ || syncAxis == SyncAxis.SyncAllYZ || syncAxis == SyncAxis.SyncAllXYZ;

            bool syncRotX = syncAxis == SyncAxis.SyncRotX || syncAxis == SyncAxis.SyncRotXY || syncAxis == SyncAxis.SyncRotXZ || syncAxis == SyncAxis.SyncRotXYZ || syncAxis == SyncAxis.SyncRotX || syncAxis == SyncAxis.SyncAllXY || syncAxis == SyncAxis.SyncAllXZ || syncAxis == SyncAxis.SyncAllXYZ;
            bool syncRotY = syncAxis == SyncAxis.SyncRotY || syncAxis == SyncAxis.SyncRotXY || syncAxis == SyncAxis.SyncRotYZ || syncAxis == SyncAxis.SyncRotXYZ || syncAxis == SyncAxis.SyncRotY || syncAxis == SyncAxis.SyncAllXY || syncAxis == SyncAxis.SyncAllYZ || syncAxis == SyncAxis.SyncAllXYZ;
            bool syncRotZ = syncAxis == SyncAxis.SyncRotZ || syncAxis == SyncAxis.SyncRotXZ || syncAxis == SyncAxis.SyncRotYZ || syncAxis == SyncAxis.SyncRotXYZ || syncAxis == SyncAxis.SyncRotZ || syncAxis == SyncAxis.SyncAllXZ || syncAxis == SyncAxis.SyncAllYZ || syncAxis == SyncAxis.SyncAllXYZ;

            bool syncScaX = syncAxis == SyncAxis.SyncScaleX || syncAxis == SyncAxis.SyncScaleXY || syncAxis == SyncAxis.SyncScaleXZ || syncAxis == SyncAxis.SyncScaleXYZ || syncAxis == SyncAxis.SyncAllX || syncAxis == SyncAxis.SyncAllXY || syncAxis == SyncAxis.SyncAllXZ || syncAxis == SyncAxis.SyncAllXYZ;
            bool syncScaY = syncAxis == SyncAxis.SyncScaleY || syncAxis == SyncAxis.SyncScaleXY || syncAxis == SyncAxis.SyncScaleYZ || syncAxis == SyncAxis.SyncScaleXYZ || syncAxis == SyncAxis.SyncAllY || syncAxis == SyncAxis.SyncAllXY || syncAxis == SyncAxis.SyncAllYZ || syncAxis == SyncAxis.SyncAllXYZ;
            bool syncScaZ = syncAxis == SyncAxis.SyncScaleZ || syncAxis == SyncAxis.SyncScaleXZ || syncAxis == SyncAxis.SyncScaleYZ || syncAxis == SyncAxis.SyncScaleXYZ || syncAxis == SyncAxis.SyncAllZ || syncAxis == SyncAxis.SyncAllXZ || syncAxis == SyncAxis.SyncAllYZ || syncAxis == SyncAxis.SyncAllXYZ;

            var gameObject = new GameObject($"Test-{nameof(NetworkTransformStateTests)}.{nameof(TestSyncAxes)}");
            var networkObject = gameObject.AddComponent<NetworkObject>();
            var networkTransform = gameObject.AddComponent<NetworkTransform>();

            var manager = new GameObject($"Test-{nameof(NetworkManager)}.{nameof(TestSyncAxes)}");
            var networkManager = manager.AddComponent<NetworkManager>();
            networkManager.NetworkConfig = new NetworkConfig();

            networkObject.NetworkManagerOwner = networkManager;

            networkTransform.enabled = false; // do not tick `FixedUpdate()` or `Update()`

            var initialPosition = Vector3.zero;
            var initialRotAngles = Vector3.zero;
            var initialScale = Vector3.one;
            networkTransform.UseHalfFloatPrecision = m_Precision == Precision.Half;
            networkTransform.UseQuaternionSynchronization = m_Rotation == Rotation.Quaternion;
            networkTransform.transform.position = initialPosition;
            networkTransform.transform.eulerAngles = initialRotAngles;
            networkTransform.transform.localScale = initialScale;
            networkTransform.SyncPositionX = syncPosX;
            networkTransform.SyncPositionY = syncPosY;
            networkTransform.SyncPositionZ = syncPosZ;
            networkTransform.SyncRotAngleX = syncRotX;
            networkTransform.SyncRotAngleY = syncRotY;
            networkTransform.SyncRotAngleZ = syncRotZ;
            networkTransform.SyncScaleX = syncScaX;
            networkTransform.SyncScaleY = syncScaY;
            networkTransform.SyncScaleZ = syncScaZ;
            networkTransform.InLocalSpace = inLocalSpace;

            // We want a relatively clean networkTransform state before we try to apply the transform to it
            // We only preserve InLocalSpace and IsTeleportingNextFrame properties as they are the only things
            // needed when applying a transform to a NetworkTransformState
            var networkTransformState = new NetworkTransformState
            {
                FlagStates = new FlagStates()
                {
                    InLocalSpace = inLocalSpace,
                    IsTeleportingNextFrame = isTeleporting,
                },
                NetworkDeltaPosition = new NetworkDeltaPosition(Vector3.zero, 0)
            };

            // Step 1: change properties, expect state to be dirty
            {
                networkTransform.transform.position = new Vector3(3, 4, 5);
                networkTransform.transform.eulerAngles = new Vector3(30, 45, 90);
                networkTransform.transform.localScale = new Vector3(1.1f, 0.5f, 2.5f);

                if (syncPosX || syncPosY || syncPosZ || syncRotX || syncRotY || syncRotZ || syncScaX || syncScaY || syncScaZ)
                {
                    Assert.NotNull(networkTransform.NetworkManager, "NetworkManager is NULL!");
                    Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                }
            }

            // We want to start with a fresh NetworkTransformState since it could have other state
            // information from the last time we applied the transform
            networkTransformState = new NetworkTransformState
            {
                FlagStates = new FlagStates()
                {
                    InLocalSpace = inLocalSpace,
                    IsTeleportingNextFrame = isTeleporting,
                }
            };
            var position = networkTransform.transform.position;
            var rotAngles = networkTransform.transform.eulerAngles;
            var scale = networkTransform.transform.localScale;

            // Step 2: Verify the state changes in a tick are additive
            // TODO: This will need to change if we update NetworkTransform to send all of the
            // axis deltas that happened over a tick as a collection instead of collapsing them
            // as the changes are detected.
            {
                networkTransformState = new NetworkTransformState
                {
                    FlagStates = new FlagStates()
                    {
                        InLocalSpace = inLocalSpace,
                        IsTeleportingNextFrame = isTeleporting,
                    }
                };

                // SyncPositionX
                if (syncPosX)
                {
                    position.x++;
                    networkTransform.transform.position = position;
                    Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    Assert.IsTrue(networkTransformState.HasPositionX);
                }

                // SyncPositionY
                if (syncPosY)
                {
                    position = networkTransform.transform.position;
                    position.y++;
                    networkTransform.transform.position = position;
                    Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    Assert.IsTrue(networkTransformState.HasPositionX || !syncPosX);
                    Assert.IsTrue(networkTransformState.HasPositionY);
                }

                // SyncPositionZ
                if (syncPosZ)
                {
                    position = networkTransform.transform.position;
                    position.z++;
                    networkTransform.transform.position = position;
                    Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    Assert.IsTrue(networkTransformState.HasPositionX || !syncPosX);
                    Assert.IsTrue(networkTransformState.HasPositionY || !syncPosY);
                    Assert.IsTrue(networkTransformState.HasPositionZ);
                }

                // SyncRotAngleX
                if (syncRotX)
                {
                    rotAngles = networkTransform.transform.eulerAngles;
                    rotAngles.x++;
                    networkTransform.transform.eulerAngles = rotAngles;
                    Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    Assert.IsTrue(networkTransformState.HasPositionX || !syncPosX);
                    Assert.IsTrue(networkTransformState.HasPositionY || !syncPosY);
                    Assert.IsTrue(networkTransformState.HasPositionZ || !syncPosZ);
                    Assert.IsTrue(networkTransformState.HasRotAngleX);
                }

                // SyncRotAngleY
                if (syncRotY)
                {
                    rotAngles = networkTransform.transform.eulerAngles;
                    rotAngles.y++;
                    networkTransform.transform.eulerAngles = rotAngles;
                    Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    Assert.IsTrue(networkTransformState.HasPositionX || !syncPosX);
                    Assert.IsTrue(networkTransformState.HasPositionY || !syncPosY);
                    Assert.IsTrue(networkTransformState.HasPositionZ || !syncPosZ);
                    Assert.IsTrue(networkTransformState.HasRotAngleX || !syncRotX);
                    Assert.IsTrue(networkTransformState.HasRotAngleY);
                }
                // SyncRotAngleZ
                if (syncRotZ)
                {
                    rotAngles = networkTransform.transform.eulerAngles;
                    rotAngles.z++;
                    networkTransform.transform.eulerAngles = rotAngles;
                    Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    Assert.IsTrue(networkTransformState.HasPositionX || !syncPosX);
                    Assert.IsTrue(networkTransformState.HasPositionY || !syncPosY);
                    Assert.IsTrue(networkTransformState.HasPositionZ || !syncPosZ);
                    Assert.IsTrue(networkTransformState.HasRotAngleX || !syncRotX);
                    Assert.IsTrue(networkTransformState.HasRotAngleY || !syncRotY);
                    Assert.IsTrue(networkTransformState.HasRotAngleZ);
                }

                // SyncScaleX
                if (syncScaX)
                {
                    scale = networkTransform.transform.localScale;
                    scale.x++;
                    networkTransform.transform.localScale = scale;
                    Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    Assert.IsTrue(networkTransformState.HasPositionX || !syncPosX);
                    Assert.IsTrue(networkTransformState.HasPositionY || !syncPosY);
                    Assert.IsTrue(networkTransformState.HasPositionZ || !syncPosZ);
                    Assert.IsTrue(networkTransformState.HasRotAngleX || !syncRotX);
                    Assert.IsTrue(networkTransformState.HasRotAngleY || !syncRotY);
                    Assert.IsTrue(networkTransformState.HasRotAngleZ || !syncRotZ);
                    Assert.IsTrue(networkTransformState.HasScaleX);
                }
                // SyncScaleY
                if (syncScaY)
                {
                    scale = networkTransform.transform.localScale;
                    scale.y++;
                    networkTransform.transform.localScale = scale;
                    Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    Assert.IsTrue(networkTransformState.HasPositionX || !syncPosX);
                    Assert.IsTrue(networkTransformState.HasPositionY || !syncPosY);
                    Assert.IsTrue(networkTransformState.HasPositionZ || !syncPosZ);
                    Assert.IsTrue(networkTransformState.HasRotAngleX || !syncRotX);
                    Assert.IsTrue(networkTransformState.HasRotAngleY || !syncRotY);
                    Assert.IsTrue(networkTransformState.HasRotAngleZ || !syncRotZ);
                    Assert.IsTrue(networkTransformState.HasScaleX || !syncScaX);
                    Assert.IsTrue(networkTransformState.HasScaleY);
                }
                // SyncScaleZ
                if (syncScaZ)
                {
                    scale = networkTransform.transform.localScale;
                    scale.z++;
                    networkTransform.transform.localScale = scale;
                    Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    Assert.IsTrue(networkTransformState.HasPositionX || !syncPosX);
                    Assert.IsTrue(networkTransformState.HasPositionY || !syncPosY);
                    Assert.IsTrue(networkTransformState.HasPositionZ || !syncPosZ);
                    Assert.IsTrue(networkTransformState.HasRotAngleX || !syncRotX);
                    Assert.IsTrue(networkTransformState.HasRotAngleY || !syncRotY);
                    Assert.IsTrue(networkTransformState.HasRotAngleZ || !syncRotZ);
                    Assert.IsTrue(networkTransformState.HasScaleX || !syncScaX);
                    Assert.IsTrue(networkTransformState.HasScaleY || !syncScaY);
                    Assert.IsTrue(networkTransformState.HasScaleZ);
                }
            }

            // Step 3: disable a particular sync flag, expect state to be not dirty
            // We do this last because it changes which axis will be synchronized.
            {
                // Reset the NetworkTransformState since teleporting will preserve
                // any dirty values
                networkTransformState = new NetworkTransformState
                {
                    FlagStates = new FlagStates()
                    {
                        InLocalSpace = inLocalSpace,
                        IsTeleportingNextFrame = isTeleporting,
                    }
                };

                position = networkTransform.transform.position;
                rotAngles = networkTransform.transform.eulerAngles;
                scale = networkTransform.transform.localScale;

                // SyncPositionX
                if (syncPosX)
                {
                    networkTransform.SyncPositionX = false;

                    position.x++;
                    networkTransform.transform.position = position;

                    // If we are synchronizing more than 1 axis (teleporting impacts this too)
                    if (syncAxis != SyncAxis.SyncPosX && WillAnAxisBeSynchronized(ref networkTransform))
                    {
                        // For the x axis position value We should expect the state to still be considered dirty (more than one axis is being synchronized and we are teleporting)
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        // However, we expect it to not have applied the position x delta
                        Assert.IsFalse(networkTransformState.HasPositionX);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

                // Reset the NetworkTransformState since teleporting will preserve
                // any dirty values
                networkTransformState = new NetworkTransformState
                {
                    FlagStates = new FlagStates()
                    {
                        InLocalSpace = inLocalSpace,
                        IsTeleportingNextFrame = isTeleporting,
                    }
                };
                // SyncPositionY
                if (syncPosY)
                {
                    networkTransform.SyncPositionY = false;

                    position.y++;
                    networkTransform.transform.position = position;
                    if (syncAxis != SyncAxis.SyncPosY && WillAnAxisBeSynchronized(ref networkTransform))
                    {
                        // We want to start with a fresh NetworkTransformState since it could have other state
                        // information from the last time we applied the transform
                        networkTransformState = new NetworkTransformState
                        {
                            FlagStates = new FlagStates()
                            {
                                InLocalSpace = inLocalSpace,
                                IsTeleportingNextFrame = isTeleporting,
                            },
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasPositionY);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

                // Reset the NetworkTransformState since teleporting will preserve
                // any dirty values
                networkTransformState = new NetworkTransformState
                {
                    FlagStates = new FlagStates()
                    {
                        InLocalSpace = inLocalSpace,
                        IsTeleportingNextFrame = isTeleporting,
                    },
                };
                // SyncPositionZ
                if (syncPosZ)
                {
                    networkTransform.SyncPositionZ = false;

                    position.z++;
                    networkTransform.transform.position = position;
                    if (syncAxis != SyncAxis.SyncPosZ && WillAnAxisBeSynchronized(ref networkTransform))
                    {
                        // We want to start with a fresh NetworkTransformState since it could have other state
                        // information from the last time we applied the transform
                        networkTransformState = new NetworkTransformState
                        {
                            FlagStates = new FlagStates()
                            {
                                InLocalSpace = inLocalSpace,
                                IsTeleportingNextFrame = isTeleporting,
                            },
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasPositionZ);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

                // Reset the NetworkTransformState since teleporting will preserve
                // any dirty values
                networkTransformState = new NetworkTransformState
                {
                    FlagStates = new FlagStates()
                    {
                        InLocalSpace = inLocalSpace,
                        IsTeleportingNextFrame = isTeleporting,
                    },
                };
                // SyncRotAngleX - Now test that we don't synchronize this specific axis as long as we are not using quaternion synchronization
                if (syncRotX && m_Rotation == Rotation.Euler)
                {
                    networkTransform.SyncRotAngleX = false;

                    rotAngles.x++;
                    networkTransform.transform.eulerAngles = rotAngles;
                    if (syncAxis != SyncAxis.SyncRotX && WillAnAxisBeSynchronized(ref networkTransform))
                    {
                        // We want to start with a fresh NetworkTransformState since it could have other state
                        // information from the last time we applied the transform
                        networkTransformState = new NetworkTransformState
                        {
                            FlagStates = new FlagStates()
                            {
                                InLocalSpace = inLocalSpace,
                                IsTeleportingNextFrame = isTeleporting,
                            },
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasRotAngleX);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

                // Reset the NetworkTransformState since teleporting will preserve
                // any dirty values
                networkTransformState = new NetworkTransformState
                {
                    FlagStates = new FlagStates()
                    {
                        InLocalSpace = inLocalSpace,
                        IsTeleportingNextFrame = isTeleporting,
                    },
                };
                // SyncRotAngleY - Now test that we don't synchronize this specific axis as long as we are not using quaternion synchronization
                if (syncRotY && m_Rotation == Rotation.Euler)
                {
                    networkTransform.SyncRotAngleY = false;

                    rotAngles.y++;
                    networkTransform.transform.eulerAngles = rotAngles;
                    if (syncAxis != SyncAxis.SyncRotY && WillAnAxisBeSynchronized(ref networkTransform))
                    {
                        // We want to start with a fresh NetworkTransformState since it could have other state
                        // information from the last time we applied the transform
                        networkTransformState = new NetworkTransformState
                        {
                            FlagStates = new FlagStates()
                            {
                                InLocalSpace = inLocalSpace,
                                IsTeleportingNextFrame = isTeleporting,
                            },
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasRotAngleY);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

                // Reset the NetworkTransformState since teleporting will preserve
                // any dirty values
                networkTransformState = new NetworkTransformState
                {
                    FlagStates = new FlagStates()
                    {
                        InLocalSpace = inLocalSpace,
                        IsTeleportingNextFrame = isTeleporting,
                    },
                };
                // SyncRotAngleZ - Now test that we don't synchronize this specific axis as long as we are not using quaternion synchronization
                if (syncRotZ && m_Rotation == Rotation.Euler)
                {
                    networkTransform.SyncRotAngleZ = false;

                    rotAngles.z++;
                    networkTransform.transform.eulerAngles = rotAngles;
                    if (syncAxis != SyncAxis.SyncRotZ && WillAnAxisBeSynchronized(ref networkTransform))
                    {
                        // We want to start with a fresh NetworkTransformState since it could have other state
                        // information from the last time we applied the transform
                        networkTransformState = new NetworkTransformState
                        {
                            FlagStates = new FlagStates()
                            {
                                InLocalSpace = inLocalSpace,
                                IsTeleportingNextFrame = isTeleporting,
                            },
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasRotAngleZ);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

                // Reset the NetworkTransformState since teleporting will preserve
                // any dirty values
                networkTransformState = new NetworkTransformState
                {
                    FlagStates = new FlagStates()
                    {
                        InLocalSpace = inLocalSpace,
                        IsTeleportingNextFrame = isTeleporting,
                    },
                };
                // SyncScaleX
                if (syncScaX)
                {
                    networkTransform.SyncScaleX = false;

                    scale.x++;
                    networkTransform.transform.localScale = scale;
                    if (syncAxis != SyncAxis.SyncScaleX && WillAnAxisBeSynchronized(ref networkTransform))
                    {
                        // We want to start with a fresh NetworkTransformState since it could have other state
                        // information from the last time we applied the transform
                        networkTransformState = new NetworkTransformState
                        {
                            FlagStates = new FlagStates()
                            {
                                InLocalSpace = inLocalSpace,
                                IsTeleportingNextFrame = isTeleporting,
                            },
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasScaleX);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

                // Reset the NetworkTransformState since teleporting will preserve
                // any dirty values
                networkTransformState = new NetworkTransformState
                {
                    FlagStates = new FlagStates()
                    {
                        InLocalSpace = inLocalSpace,
                        IsTeleportingNextFrame = isTeleporting,
                    },
                };
                // SyncScaleY
                if (syncScaY)
                {
                    networkTransform.SyncScaleY = false;

                    scale.y++;
                    networkTransform.transform.localScale = scale;
                    if (syncAxis != SyncAxis.SyncScaleY && WillAnAxisBeSynchronized(ref networkTransform))
                    {
                        // We want to start with a fresh NetworkTransformState since it could have other state
                        // information from the last time we applied the transform
                        networkTransformState = new NetworkTransformState
                        {
                            FlagStates = new FlagStates()
                            {
                                InLocalSpace = inLocalSpace,
                                IsTeleportingNextFrame = isTeleporting,
                            },
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasScaleY);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

                // Reset the NetworkTransformState since teleporting will preserve
                // any dirty values
                networkTransformState = new NetworkTransformState
                {
                    FlagStates = new FlagStates()
                    {
                        InLocalSpace = inLocalSpace,
                        IsTeleportingNextFrame = isTeleporting,
                    },
                };
                // SyncScaleZ
                if (syncScaZ)
                {
                    networkTransform.SyncScaleZ = false;

                    scale.z++;
                    networkTransform.transform.localScale = scale;
                    if (syncAxis != SyncAxis.SyncScaleZ && WillAnAxisBeSynchronized(ref networkTransform))
                    {
                        // We want to start with a fresh NetworkTransformState since it could have other state
                        // information from the last time we applied the transform
                        networkTransformState = new NetworkTransformState
                        {
                            FlagStates = new FlagStates()
                            {
                                InLocalSpace = inLocalSpace,
                                IsTeleportingNextFrame = isTeleporting,
                            }
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasScaleZ);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

            }

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(manager);
        }


        [Test]
        public void TestThresholds(
            [Values(PositionThresholdDefault, 1.0f)] float positionThreshold,
            [Values(RotAngleThresholdDefault, 1.0f)] float rotAngleThreshold,
            [Values(ScaleThresholdDefault, 0.5f)] float scaleThreshold)
        {
            var inLocalSpace = m_TransformSpace == TransformSpace.Local;
            var gameObject = new GameObject($"Test-{nameof(NetworkTransformStateTests)}.{nameof(TestThresholds)}");
            var networkTransform = gameObject.AddComponent<NetworkTransform>();
            networkTransform.enabled = false; // do not tick `FixedUpdate()` or `Update()`

            var initialPosition = Vector3.zero;
            var initialRotAngles = Vector3.zero;
            var initialScale = Vector3.one;

            networkTransform.transform.position = initialPosition;
            networkTransform.transform.eulerAngles = initialRotAngles;
            networkTransform.transform.localScale = initialScale;
            networkTransform.SyncPositionX = true;
            networkTransform.SyncPositionY = true;
            networkTransform.SyncPositionZ = true;
            networkTransform.SyncRotAngleX = true;
            networkTransform.SyncRotAngleY = true;
            networkTransform.SyncRotAngleZ = true;
            networkTransform.SyncScaleX = true;
            networkTransform.SyncScaleY = true;
            networkTransform.SyncScaleZ = true;
            networkTransform.InLocalSpace = inLocalSpace;
            networkTransform.PositionThreshold = positionThreshold;
            networkTransform.RotAngleThreshold = rotAngleThreshold;
            networkTransform.ScaleThreshold = scaleThreshold;

            var networkTransformState = new NetworkTransformState
            {
                PositionX = initialPosition.x,
                PositionY = initialPosition.y,
                PositionZ = initialPosition.z,
                RotAngleX = initialRotAngles.x,
                RotAngleY = initialRotAngles.y,
                RotAngleZ = initialRotAngles.z,
                ScaleX = initialScale.x,
                ScaleY = initialScale.y,
                ScaleZ = initialScale.z,
                FlagStates = new FlagStates()
                {
                    InLocalSpace = inLocalSpace,
                },
            };

            // Step 1: change properties, expect state to be dirty
            {
                networkTransform.transform.position = new Vector3(3, 4, 5);
                networkTransform.transform.eulerAngles = new Vector3(30, 45, 90);
                networkTransform.transform.localScale = new Vector3(1.1f, 0.5f, 2.5f);

                Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
            }

            // Step 2: make changes below and above thresholds
            // changes below the threshold should not make `NetworkState` dirty
            // changes above the threshold should make `NetworkState` dirty
            {
                // Position
                if (!Mathf.Approximately(positionThreshold, 0.0f))
                {
                    var position = networkTransform.transform.position;

                    // PositionX
                    {
                        position.x += positionThreshold / 2;
                        networkTransform.transform.position = position;
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));

                        position.x += positionThreshold * 2;
                        networkTransform.transform.position = position;
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }

                    // PositionY
                    {
                        position.y += positionThreshold / 2;
                        networkTransform.transform.position = position;
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));

                        position.y += positionThreshold * 2;
                        networkTransform.transform.position = position;
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }

                    // PositionZ
                    {
                        position.z += positionThreshold / 2;
                        networkTransform.transform.position = position;
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));

                        position.z += positionThreshold * 2;
                        networkTransform.transform.position = position;
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

                // RotAngles
                if (!Mathf.Approximately(rotAngleThreshold, 0.0f))
                {
                    var rotAngles = networkTransform.transform.eulerAngles;

                    // RotAngleX
                    {
                        rotAngles.x += rotAngleThreshold / 2;
                        networkTransform.transform.eulerAngles = rotAngles;
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));

                        rotAngles.x += rotAngleThreshold * 2;
                        networkTransform.transform.eulerAngles = rotAngles;
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }

                    // RotAngleY
                    {
                        rotAngles.y += rotAngleThreshold / 2;
                        networkTransform.transform.eulerAngles = rotAngles;
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));

                        rotAngles.y += rotAngleThreshold * 2;
                        networkTransform.transform.eulerAngles = rotAngles;
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }

                    // RotAngleZ
                    {
                        rotAngles.z += rotAngleThreshold / 2;
                        networkTransform.transform.eulerAngles = rotAngles;
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));

                        rotAngles.z += rotAngleThreshold * 2;
                        networkTransform.transform.eulerAngles = rotAngles;
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

                // Scale
                if (!Mathf.Approximately(scaleThreshold, 0.0f) && inLocalSpace)
                {
                    var scale = networkTransform.transform.localScale;

                    // ScaleX
                    {
                        scale.x += scaleThreshold / 2;
                        networkTransform.transform.localScale = scale;
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));

                        scale.x += scaleThreshold * 2;
                        networkTransform.transform.localScale = scale;
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }

                    // ScaleY
                    {
                        scale.y += scaleThreshold / 2;
                        networkTransform.transform.localScale = scale;
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));

                        scale.y += scaleThreshold * 2;
                        networkTransform.transform.localScale = scale;
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }

                    // ScaleZ
                    {
                        scale.z += scaleThreshold / 2;
                        networkTransform.transform.localScale = scale;
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));

                        scale.z += scaleThreshold * 2;
                        networkTransform.transform.localScale = scale;
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }
            }

            Object.DestroyImmediate(gameObject);
        }
    }
}
