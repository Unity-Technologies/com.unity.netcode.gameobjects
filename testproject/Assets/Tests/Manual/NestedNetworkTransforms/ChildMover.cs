using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class ChildMover : NetworkBehaviour
    {
        public static bool RandomizeScale;

        [Range(0.1f, 30.0f)]
        public float RotationSpeed = 5.0f;

        private Transform m_RootParentTransform;

        public void PlayerIsMoving(float movementDirection)
        {
            if (IsSpawned && IsOwner)
            {
                var rotateDirection = movementDirection * RotationSpeed;
                transform.RotateAround(m_RootParentTransform.position, Vector3.up, rotateDirection);
            }
        }

        private Transform GetRootParentTransform(Transform transform)
        {
            if (transform.parent != null)
            {
                return GetRootParentTransform(transform.parent);
            }
            return transform;
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                if (RandomizeScale)
                {
                    transform.localScale = transform.localScale * Random.Range(0.75f, 1.75f);
                }
                m_RootParentTransform = GetRootParentTransform(transform);
            }
            base.OnNetworkSpawn();
        }
    }
}
