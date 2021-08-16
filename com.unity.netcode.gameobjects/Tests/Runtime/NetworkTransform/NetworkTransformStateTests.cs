using NUnit.Framework;
using Unity.Netcode.Prototyping;
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

            networkTransform.ReplNetworkState.Value = new NetworkTransform.NetworkState
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
                networkTransform.InLocalSpace = !inLocalSpace;
                networkTransform.transform.position = new Vector3(3, 4, 5);
                networkTransform.transform.eulerAngles = new Vector3(30, 45, 90);
                networkTransform.transform.localScale = new Vector3(1.1f, 0.5f, 2.5f);

                bool isDirty = networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value);
                networkTransform.ReplNetworkState.SetDirty(isDirty);
                Assert.IsTrue(isDirty);
            }

            // Step 2: apply current state locally, expect state to be not dirty/different
            {
                networkTransform.ApplyNetworkState(networkTransform.ReplNetworkState.Value);

                bool isDirty = networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value);
                Assert.IsFalse(isDirty);
            }

            // Step 3: disable a particular sync flag, expect state to be not dirty
            {
                var position = networkTransform.transform.position;
                var rotAngles = networkTransform.transform.eulerAngles;
                var scale = networkTransform.transform.localScale;

                // SyncPositionX
                {
                    networkTransform.SyncPositionX = false;

                    position.x++;
                    networkTransform.transform.position = position;

                    Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                }
                // SyncPositionY
                {
                    networkTransform.SyncPositionY = false;

                    position.y++;
                    networkTransform.transform.position = position;

                    Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                }
                // SyncPositionZ
                {
                    networkTransform.SyncPositionZ = false;

                    position.z++;
                    networkTransform.transform.position = position;

                    Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                }

                // SyncRotAngleX
                {
                    networkTransform.SyncRotAngleX = false;

                    rotAngles.x++;
                    networkTransform.transform.eulerAngles = rotAngles;

                    Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                }
                // SyncRotAngleY
                {
                    networkTransform.SyncRotAngleY = false;

                    rotAngles.y++;
                    networkTransform.transform.eulerAngles = rotAngles;

                    Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                }
                // SyncRotAngleZ
                {
                    networkTransform.SyncRotAngleZ = false;

                    rotAngles.z++;
                    networkTransform.transform.eulerAngles = rotAngles;

                    Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                }

                // SyncScaleX
                {
                    networkTransform.SyncScaleX = false;

                    scale.x++;
                    networkTransform.transform.localScale = scale;

                    Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                }
                // SyncScaleY
                {
                    networkTransform.SyncScaleY = false;

                    scale.y++;
                    networkTransform.transform.localScale = scale;

                    Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                }
                // SyncScaleZ
                {
                    networkTransform.SyncScaleZ = false;

                    scale.z++;
                    networkTransform.transform.localScale = scale;

                    Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                }
            }

            Object.DestroyImmediate(networkTransform);
        }

        [Test]
        public void TestThresholds(
            [Values] bool inLocalSpace,
            [Values(0, 1.0f)] float positionThreshold,
            [Values(0, 1.0f)] float rotAngleThreshold,
            [Values(0, 0.5f)] float scaleThreshold)
        {
            var gameObject = new GameObject($"Test-{nameof(NetworkTransformStateTests)}.{nameof(TestThresholds)}");
            var networkObject = gameObject.AddComponent<NetworkObject>();
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

            networkTransform.ReplNetworkState.Value = new NetworkTransform.NetworkState
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
                networkTransform.InLocalSpace = !inLocalSpace;
                networkTransform.transform.position = new Vector3(3, 4, 5);
                networkTransform.transform.eulerAngles = new Vector3(30, 45, 90);
                networkTransform.transform.localScale = new Vector3(1.1f, 0.5f, 2.5f);

                bool isDirty = networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value);
                networkTransform.ReplNetworkState.SetDirty(isDirty);
                Assert.IsTrue(isDirty);
            }

            // Step 2: apply current state locally, expect state to be not dirty/different
            {
                networkTransform.ApplyNetworkState(networkTransform.ReplNetworkState.Value);

                bool isDirty = networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value);
                Assert.IsFalse(isDirty);
            }

            // Step 3: make changes below and above thresholds
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
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        position.x += positionThreshold * 2;
                        networkTransform.transform.position = position;
                        Assert.IsTrue(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        networkTransform.ApplyNetworkState(networkTransform.ReplNetworkState.Value);
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                    }

                    // PositionY
                    {
                        position.y += positionThreshold / 2;
                        networkTransform.transform.position = position;
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        position.y += positionThreshold * 2;
                        networkTransform.transform.position = position;
                        Assert.IsTrue(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        networkTransform.ApplyNetworkState(networkTransform.ReplNetworkState.Value);
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                    }

                    // PositionZ
                    {
                        position.z += positionThreshold / 2;
                        networkTransform.transform.position = position;
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        position.z += positionThreshold * 2;
                        networkTransform.transform.position = position;
                        Assert.IsTrue(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        networkTransform.ApplyNetworkState(networkTransform.ReplNetworkState.Value);
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
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
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        rotAngles.x += rotAngleThreshold * 2;
                        networkTransform.transform.eulerAngles = rotAngles;
                        Assert.IsTrue(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        networkTransform.ApplyNetworkState(networkTransform.ReplNetworkState.Value);
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                    }

                    // RotAngleY
                    {
                        rotAngles.y += rotAngleThreshold / 2;
                        networkTransform.transform.eulerAngles = rotAngles;
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        rotAngles.y += rotAngleThreshold * 2;
                        networkTransform.transform.eulerAngles = rotAngles;
                        Assert.IsTrue(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        networkTransform.ApplyNetworkState(networkTransform.ReplNetworkState.Value);
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                    }

                    // RotAngleZ
                    {
                        rotAngles.z += rotAngleThreshold / 2;
                        networkTransform.transform.eulerAngles = rotAngles;
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        rotAngles.z += rotAngleThreshold * 2;
                        networkTransform.transform.eulerAngles = rotAngles;
                        Assert.IsTrue(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        networkTransform.ApplyNetworkState(networkTransform.ReplNetworkState.Value);
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
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
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        scale.x += scaleThreshold * 2;
                        networkTransform.transform.localScale = scale;
                        Assert.IsTrue(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        networkTransform.ApplyNetworkState(networkTransform.ReplNetworkState.Value);
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                    }

                    // ScaleY
                    {
                        scale.y += scaleThreshold / 2;
                        networkTransform.transform.localScale = scale;
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        scale.y += scaleThreshold * 2;
                        networkTransform.transform.localScale = scale;
                        Assert.IsTrue(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        networkTransform.ApplyNetworkState(networkTransform.ReplNetworkState.Value);
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                    }

                    // ScaleZ
                    {
                        scale.z += scaleThreshold / 2;
                        networkTransform.transform.localScale = scale;
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        scale.z += scaleThreshold * 2;
                        networkTransform.transform.localScale = scale;
                        Assert.IsTrue(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));

                        networkTransform.ApplyNetworkState(networkTransform.ReplNetworkState.Value);
                        Assert.IsFalse(networkTransform.UpdateNetworkState(networkTransform.ReplNetworkState.Value));
                    }
                }
            }

            Object.DestroyImmediate(networkTransform);
        }
    }
}
