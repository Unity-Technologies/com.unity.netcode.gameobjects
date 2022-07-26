using NUnit.Framework;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkTransformStateTests
    {
        [Test]
        public void TestSyncAxes(
            [Values] bool inLocalSpace,
            [Values] bool syncPosX, [Values] bool syncPosY, [Values] bool syncPosZ,
            [Values] bool syncRotX, [Values] bool syncRotY, [Values] bool syncRotZ,
            [Values] bool syncScaX, [Values] bool syncScaY, [Values] bool syncScaZ)
        {
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
                HasPositionX = syncPosX,
                HasPositionY = syncPosY,
                HasPositionZ = syncPosZ,
                HasRotAngleX = syncRotX,
                HasRotAngleY = syncRotY,
                HasRotAngleZ = syncRotZ,
                HasScaleX = syncScaX,
                HasScaleY = syncScaY,
                HasScaleZ = syncScaZ,
                InLocalSpace = inLocalSpace
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

            // Step 2: disable a particular sync flag, expect state to be not dirty
            {
                var position = networkTransform.transform.position;
                var rotAngles = networkTransform.transform.eulerAngles;
                var scale = networkTransform.transform.localScale;

                // SyncPositionX
                {
                    networkTransform.SyncPositionX = false;

                    position.x++;
                    networkTransform.transform.position = position;

                    Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                }
                // SyncPositionY
                {
                    networkTransform.SyncPositionY = false;

                    position.y++;
                    networkTransform.transform.position = position;

                    Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                }
                // SyncPositionZ
                {
                    networkTransform.SyncPositionZ = false;

                    position.z++;
                    networkTransform.transform.position = position;

                    Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                }

                // SyncRotAngleX
                {
                    networkTransform.SyncRotAngleX = false;

                    rotAngles.x++;
                    networkTransform.transform.eulerAngles = rotAngles;

                    Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                }
                // SyncRotAngleY
                {
                    networkTransform.SyncRotAngleY = false;

                    rotAngles.y++;
                    networkTransform.transform.eulerAngles = rotAngles;

                    Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                }
                // SyncRotAngleZ
                {
                    networkTransform.SyncRotAngleZ = false;

                    rotAngles.z++;
                    networkTransform.transform.eulerAngles = rotAngles;

                    Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                }

                // SyncScaleX
                {
                    networkTransform.SyncScaleX = false;

                    scale.x++;
                    networkTransform.transform.localScale = scale;

                    Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                }
                // SyncScaleY
                {
                    networkTransform.SyncScaleY = false;

                    scale.y++;
                    networkTransform.transform.localScale = scale;

                    Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                }
                // SyncScaleZ
                {
                    networkTransform.SyncScaleZ = false;

                    scale.z++;
                    networkTransform.transform.localScale = scale;

                    Assert.IsFalse(networkTransform.ApplyTransformToNetworkState(ref networkTransformState, 0, networkTransform.transform));
                }
            }

            Object.DestroyImmediate(gameObject);
        }


        [Test]
        public void TestThresholds(
            [Values] bool inLocalSpace,
            [Values(NetworkTransform.PositionThresholdDefault, 1.0f)] float positionThreshold,
            [Values(NetworkTransform.RotAngleThresholdDefault, 1.0f)] float rotAngleThreshold,
            [Values(NetworkTransform.ScaleThresholdDefault, 0.5f)] float scaleThreshold)
        {
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
