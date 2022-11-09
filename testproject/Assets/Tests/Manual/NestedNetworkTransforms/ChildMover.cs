using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class ChildMover : NetworkBehaviour
    {
        public static bool RandomizeScale;

        [Range(0.1f, 30.0f)]
        public float RotationSpeed = 5.0f;

        public void PlayerIsMoving(float movementDirection)
        {
            if (IsSpawned && IsOwner)
            {
                var rotateDirection = movementDirection * RotationSpeed;
                transform.RotateAround(transform.parent.position, Vector3.up, rotateDirection);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner && RandomizeScale)
            {
                transform.localScale = transform.localScale * Random.Range(0.5f, 2.0f);
            }
            base.OnNetworkSpawn();
        }
    }
}
