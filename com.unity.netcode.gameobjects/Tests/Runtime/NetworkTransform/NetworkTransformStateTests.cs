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
            var gameObject = new GameObject($"Test-{nameof(NetworkTransformStateTests)}");
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.GlobalObjectIdHash = (uint)Time.realtimeSinceStartup;
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

            // Step 1: change properties, expect state to be dirty (tests comparison)
            {
                networkTransform.InLocalSpace = !inLocalSpace;
                networkTransform.transform.position = new Vector3(3, 4, 5);
                networkTransform.transform.eulerAngles = new Vector3(30, 45, 90);
                networkTransform.transform.localScale = new Vector3(1.1f, 0.5f, 2.5f);

                Assert.IsTrue(networkTransform.IsNetworkStateDirty(networkTransform.ReplNetworkState.Value));
            }

            // Step 2: update state, expect netvar to be dirty (tests serialization)
            {
                networkTransform.UpdateNetworkState();

                Assert.IsTrue(networkTransform.ReplNetworkState.IsDirty());
            }

            // Step 3: apply current state locally, expect state to be not dirty/different (tests deserialization)
            {
                networkTransform.ApplyNetworkState(networkTransform.ReplNetworkState.Value);

                Assert.IsFalse(networkTransform.IsNetworkStateDirty(networkTransform.ReplNetworkState.Value));
            }

            // Step 4: disable a particular sync flag, expect state to be not dirty (tests individual sync flags)
            {
                // SyncPositionX
                {
                    networkTransform.SyncPositionX = false;

                    var position = networkTransform.transform.position;
                    position.x++;
                    networkTransform.transform.position = position;

                    Assert.IsFalse(networkTransform.IsNetworkStateDirty(networkTransform.ReplNetworkState.Value));
                }
                // SyncPositionY
                {
                    networkTransform.SyncPositionY = false;

                    var position = networkTransform.transform.position;
                    position.y++;
                    networkTransform.transform.position = position;

                    Assert.IsFalse(networkTransform.IsNetworkStateDirty(networkTransform.ReplNetworkState.Value));
                }
                // SyncPositionZ
                {
                    networkTransform.SyncPositionZ = false;

                    var position = networkTransform.transform.position;
                    position.z++;
                    networkTransform.transform.position = position;

                    Assert.IsFalse(networkTransform.IsNetworkStateDirty(networkTransform.ReplNetworkState.Value));
                }
                // SyncRotAngleX
                {
                    networkTransform.SyncRotAngleX = false;

                    var rotationAngles = networkTransform.transform.eulerAngles;
                    rotationAngles.x++;
                    networkTransform.transform.eulerAngles = rotationAngles;

                    Assert.IsFalse(networkTransform.IsNetworkStateDirty(networkTransform.ReplNetworkState.Value));
                }
                // SyncRotAngleY
                {
                    networkTransform.SyncRotAngleY = false;

                    var rotationAngles = networkTransform.transform.eulerAngles;
                    rotationAngles.y++;
                    networkTransform.transform.eulerAngles = rotationAngles;

                    Assert.IsFalse(networkTransform.IsNetworkStateDirty(networkTransform.ReplNetworkState.Value));
                }
                // SyncRotAngleZ
                {
                    networkTransform.SyncRotAngleZ = false;

                    var rotationAngles = networkTransform.transform.eulerAngles;
                    rotationAngles.z++;
                    networkTransform.transform.eulerAngles = rotationAngles;

                    Assert.IsFalse(networkTransform.IsNetworkStateDirty(networkTransform.ReplNetworkState.Value));
                }
                // SyncScaleX
                {
                    networkTransform.SyncScaleX = false;

                    var scale = networkTransform.transform.localScale;
                    scale.x++;
                    networkTransform.transform.localScale = scale;

                    Assert.IsFalse(networkTransform.IsNetworkStateDirty(networkTransform.ReplNetworkState.Value));
                }
                // SyncScaleY
                {
                    networkTransform.SyncScaleY = false;

                    var scale = networkTransform.transform.localScale;
                    scale.y++;
                    networkTransform.transform.localScale = scale;

                    Assert.IsFalse(networkTransform.IsNetworkStateDirty(networkTransform.ReplNetworkState.Value));
                }
                // SyncScaleZ
                {
                    networkTransform.SyncScaleZ = false;

                    var scale = networkTransform.transform.localScale;
                    scale.z++;
                    networkTransform.transform.localScale = scale;

                    Assert.IsFalse(networkTransform.IsNetworkStateDirty(networkTransform.ReplNetworkState.Value));
                }
            }

            Object.DestroyImmediate(gameObject);
        }
    }
}
