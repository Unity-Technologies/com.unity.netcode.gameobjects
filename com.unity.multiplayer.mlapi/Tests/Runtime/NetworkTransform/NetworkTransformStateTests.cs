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
            var networkTransform = gameObject.AddComponent<NetworkTransform>();
            networkTransform.enabled = false; // do not tick `FixedUpdate()` or `Update()`

            var initialPosition = Vector3.zero;
            var initialRotation = Quaternion.identity;
            var initialScale = Vector3.one;

            networkTransform.transform.position = initialPosition;
            networkTransform.transform.rotation = initialRotation;
            networkTransform.transform.localScale = initialScale;
            networkTransform.SyncPositionX = syncPosX;
            networkTransform.SyncPositionY = syncPosY;
            networkTransform.SyncPositionZ = syncPosZ;
            networkTransform.SyncRotationX = syncRotX;
            networkTransform.SyncRotationY = syncRotY;
            networkTransform.SyncRotationZ = syncRotZ;
            networkTransform.SyncScaleX = syncScaX;
            networkTransform.SyncScaleY = syncScaY;
            networkTransform.SyncScaleZ = syncScaZ;
            networkTransform.InLocalSpace = inLocalSpace;

            networkTransform.ReplNetworkState.Value = new NetworkTransform.NetworkState
            {
                PositionX = initialPosition.x,
                PositionY = initialPosition.y,
                PositionZ = initialPosition.z,
                RotationX = initialRotation.x,
                RotationY = initialRotation.y,
                RotationZ = initialRotation.z,
                ScaleX = initialScale.x,
                ScaleY = initialScale.y,
                ScaleZ = initialScale.z,
                HasPositionX = syncPosX,
                HasPositionY = syncPosY,
                HasPositionZ = syncPosZ,
                HasRotationX = syncRotX,
                HasRotationY = syncRotY,
                HasRotationZ = syncRotZ,
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

            Object.DestroyImmediate(networkTransform);
        }
    }
}
