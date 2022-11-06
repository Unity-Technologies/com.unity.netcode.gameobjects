using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class ChildMover : NetworkBehaviour
    {
        [Range(0.1f, 30.0f)]
        public float RotationSpeed = 5.0f;

        public void PlayerIsMoving(bool forward)
        {
            if (IsSpawned && IsOwner)
            {
                var rotateDirection = forward ? RotationSpeed : -1 * RotationSpeed;
                transform.RotateAround(transform.parent.position, Vector3.up, rotateDirection);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }
    }
}
