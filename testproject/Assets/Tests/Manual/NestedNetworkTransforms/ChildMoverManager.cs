using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace TestProject.ManualTests
{
    public class ChildMoverManager : NetworkBehaviour
    {
        public static bool StopMovement;

        public List<ChildMover> ChildMovers;

        [Range(0.001f, 5.0f)]
        public float TriggerDistanceToMove = 0.2f;

        public Camera PlayerCamera;
        private Vector3 m_LastPosition;
        private Vector3 m_LastForward;
        private Camera m_MainCamera;

        private void Awake()
        {
            if (PlayerCamera != null)
            {
                PlayerCamera.enabled = false;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                m_LastPosition = transform.position;
                m_LastForward = transform.forward;

                for (int i = 0; i < Camera.allCamerasCount; i++)
                {
                    var camera = Camera.allCameras[i];
                    if (camera.name == "Main Camera")
                    {
                        m_MainCamera = Camera.allCameras[i];
                    }
                }
            }
            base.OnNetworkSpawn();
        }

        private bool ChildMoversHaveAuthority()
        {
            foreach (var childMover in ChildMovers)
            {
                if (!childMover.IsAuthority())
                {
                    return false;
                }
            }
            return true;
        }

        private void Update()
        {
            if (IsSpawned && !StopMovement && ChildMoversHaveAuthority())
            {
                var deltaPosition = (transform.position - m_LastPosition);
                if (deltaPosition.sqrMagnitude >= (TriggerDistanceToMove * TriggerDistanceToMove))
                {
                    // Get our movement direction
                    var movementDirection = Vector3.Dot(deltaPosition.normalized, transform.forward);
                    if (movementDirection == 0)
                    {
                        movementDirection = m_LastMovementDirection;
                    }
                    else
                    {
                        m_LastMovementDirection = movementDirection;
                    }
                    var rotationDirection = Vector3.zero;
                    if (movementDirection > 0)
                    {
                        rotationDirection = Vector3.Cross(m_LastForward, transform.forward);
                    }
                    else if (movementDirection < 0)
                    {
                        rotationDirection = Vector3.Cross(transform.forward, m_LastForward);
                    }
                    else
                    {
                        rotationDirection.y = m_LastRotDirection;
                    }

                    m_LastRotDirection = rotationDirection.y;
                    movementDirection *= rotationDirection.y < 0 ? -1 : 1;

                    m_LastPosition = transform.position;
                    m_LastForward = transform.forward;

                    foreach (var childMover in ChildMovers)
                    {
                        childMover.PlayerIsMoving(Mathf.Sign(movementDirection));
                    }
                }
            }
        }

        private float m_LastRotDirection = 1.0f;
        private float m_LastMovementDirection = 1.0f;
        private void LateUpdate()
        {
            if (IsOwner && IsSpawned && !StopMovement)
            {
                if (Input.GetKeyDown(KeyCode.C) && PlayerCamera != null && m_MainCamera != null)
                {
                    if (m_MainCamera.isActiveAndEnabled)
                    {
                        PlayerCamera.enabled = true;
                        m_MainCamera.enabled = false;
                    }
                    else
                    {
                        m_MainCamera.enabled = true;
                        PlayerCamera.enabled = false;
                    }
                }
            }
        }

    }
}
