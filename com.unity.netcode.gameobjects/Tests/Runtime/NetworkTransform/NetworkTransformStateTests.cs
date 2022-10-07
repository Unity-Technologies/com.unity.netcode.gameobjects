using NUnit.Framework;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode.RuntimeTests
{

    [TestFixture(TransformSpace.World)]
    [TestFixture(TransformSpace.Local)]
    public class NetworkTransformStateTests
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

        public enum SynchronizationType
        {
            Delta,
            Teleport
        }

        private TransformSpace m_TransformSpace;

        public NetworkTransformStateTests(TransformSpace transformSpace)
        {
            m_TransformSpace = transformSpace;
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
            networkTransform.enabled = false; // do not tick `FixedUpdate()` or `Update()`

            var initialPosition = Vector3.zero;
            var initialRotAngles = Vector3.zero;
            var initialScale = Vector3.one;

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
            var networkTransformState = new NetworkTransform.NetworkTransformState
            {
                InLocalSpace = inLocalSpace,
                IsTeleportingNextFrame = isTeleporting,
            };

            // Step 1: change properties, expect state to be dirty
            {
                networkTransform.transform.position = new Vector3(3, 4, 5);
                networkTransform.transform.eulerAngles = new Vector3(30, 45, 90);
                networkTransform.transform.localScale = new Vector3(1.1f, 0.5f, 2.5f);

                if (syncPosX || syncPosY || syncPosZ || syncRotX || syncRotY || syncRotZ || syncScaX || syncScaY || syncScaZ)
                {
                    Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                }
            }

            // We want to start with a fresh NetworkTransformState since it could have other state
            // information from the last time we applied the transform
            networkTransformState = new NetworkTransform.NetworkTransformState
            {
                InLocalSpace = inLocalSpace,
                IsTeleportingNextFrame = isTeleporting,
            };
            var position = networkTransform.transform.position;
            var rotAngles = networkTransform.transform.eulerAngles;
            var scale = networkTransform.transform.localScale;

            // Step 2: Verify the state changes in a tick are additive
            // TODO: This will need to change if we update NetworkTransform to send all of the
            // axis deltas that happened over a tick as a collection instead of collapsing them
            // as the changes are detected.
            {
                networkTransformState = new NetworkTransform.NetworkTransformState
                {
                    InLocalSpace = inLocalSpace,
                    IsTeleportingNextFrame = isTeleporting,
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
                networkTransformState = new NetworkTransform.NetworkTransformState
                {
                    InLocalSpace = inLocalSpace,
                    IsTeleportingNextFrame = isTeleporting,
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
                        networkTransformState = new NetworkTransform.NetworkTransformState
                        {
                            InLocalSpace = inLocalSpace,
                            IsTeleportingNextFrame = isTeleporting,
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasPositionY);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

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
                        networkTransformState = new NetworkTransform.NetworkTransformState
                        {
                            InLocalSpace = inLocalSpace,
                            IsTeleportingNextFrame = isTeleporting,
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasPositionZ);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

                // SyncRotAngleX
                if (syncRotX)
                {
                    networkTransform.SyncRotAngleX = false;

                    rotAngles.x++;
                    networkTransform.transform.eulerAngles = rotAngles;
                    if (syncAxis != SyncAxis.SyncRotX && WillAnAxisBeSynchronized(ref networkTransform))
                    {
                        // We want to start with a fresh NetworkTransformState since it could have other state
                        // information from the last time we applied the transform
                        networkTransformState = new NetworkTransform.NetworkTransformState
                        {
                            InLocalSpace = inLocalSpace,
                            IsTeleportingNextFrame = isTeleporting,
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasRotAngleX);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }
                // SyncRotAngleY
                if (syncRotY)
                {
                    networkTransform.SyncRotAngleY = false;

                    rotAngles.y++;
                    networkTransform.transform.eulerAngles = rotAngles;
                    if (syncAxis != SyncAxis.SyncRotY && WillAnAxisBeSynchronized(ref networkTransform))
                    {
                        // We want to start with a fresh NetworkTransformState since it could have other state
                        // information from the last time we applied the transform
                        networkTransformState = new NetworkTransform.NetworkTransformState
                        {
                            InLocalSpace = inLocalSpace,
                            IsTeleportingNextFrame = isTeleporting,
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasRotAngleY);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }
                // SyncRotAngleZ
                if (syncRotZ)
                {
                    networkTransform.SyncRotAngleZ = false;

                    rotAngles.z++;
                    networkTransform.transform.eulerAngles = rotAngles;
                    if (syncAxis != SyncAxis.SyncRotZ && WillAnAxisBeSynchronized(ref networkTransform))
                    {
                        // We want to start with a fresh NetworkTransformState since it could have other state
                        // information from the last time we applied the transform
                        networkTransformState = new NetworkTransform.NetworkTransformState
                        {
                            InLocalSpace = inLocalSpace,
                            IsTeleportingNextFrame = isTeleporting,
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasRotAngleZ);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }

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
                        networkTransformState = new NetworkTransform.NetworkTransformState
                        {
                            InLocalSpace = inLocalSpace,
                            IsTeleportingNextFrame = isTeleporting,
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasScaleX);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }
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
                        networkTransformState = new NetworkTransform.NetworkTransformState
                        {
                            InLocalSpace = inLocalSpace,
                            IsTeleportingNextFrame = isTeleporting,
                        };
                        Assert.IsTrue(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                        Assert.IsFalse(networkTransformState.HasScaleY);
                    }
                    else
                    {
                        Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                    }
                }
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
                        networkTransformState = new NetworkTransform.NetworkTransformState
                        {
                            InLocalSpace = inLocalSpace,
                            IsTeleportingNextFrame = isTeleporting,
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
        }


        [Test]
        public void TestThresholds(
            [Values(NetworkTransform.PositionThresholdDefault, 1.0f)] float positionThreshold,
            [Values(NetworkTransform.RotAngleThresholdDefault, 1.0f)] float rotAngleThreshold,
            [Values(NetworkTransform.ScaleThresholdDefault, 0.5f)] float scaleThreshold)
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

            var networkTransformState = new NetworkTransform.NetworkTransformState
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
                InLocalSpace = inLocalSpace
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
