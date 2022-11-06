using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class ChildMoverManager : NetworkBehaviour
    {
        public List<ChildMover> ChildMovers;

        [Range(0.001f, 5.0f)]
        public float TriggerDistanceToMove = 0.2f;
        private Vector3 m_LastPosition;
        private Vector3 m_LastForward;
        private Camera m_MainCamera;
        private Camera m_PlayerViewCamera;


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
                    else if (camera.name == "PlayerView")
                    {
                        m_PlayerViewCamera = camera;
                    }
                }

                if (m_PlayerViewCamera != null)
                {
                    m_PlayerViewCamera.enabled = false;
                    m_PlayerViewCamera.transform.parent = transform;
                }
            }
            base.OnNetworkSpawn();
        }

        private void Update()
        {
            if (IsOwner && IsSpawned)
            {
                var deltaPosition = (transform.position - m_LastPosition);
                if (deltaPosition.sqrMagnitude >=  (TriggerDistanceToMove * TriggerDistanceToMove))
                {
                    // Get our movement direction
                    var movementDirection = Vector3.Dot(deltaPosition.normalized, transform.forward);
                    var rotationDirection = Vector3.zero;
                    if (movementDirection >= 0)
                    {
                        rotationDirection = Vector3.Cross(m_LastForward, transform.forward);
                    }
                    else
                    {
                        rotationDirection = Vector3.Cross(transform.forward, m_LastForward);
                    }

                    movementDirection *= rotationDirection.y < 0 ? -1 : 1;
                    bool forward = true;
                    if (movementDirection < 0 )
                    {
                        forward = false;
                    }
                    m_LastPosition = transform.position;
                    m_LastForward = transform.forward;
                    foreach (var childMover in ChildMovers)
                    {
                        childMover.PlayerIsMoving(forward);
                    }
                }

                if (Input.GetKeyDown(KeyCode.P) && m_PlayerViewCamera != null && m_MainCamera != null)
                {
                    if(m_MainCamera.enabled)
                    {
                        m_MainCamera.enabled = false;
                        m_PlayerViewCamera.enabled = true;
                    }
                    else
                    {
                        m_MainCamera.enabled = true;
                        m_PlayerViewCamera.enabled = false;
                    }
                }
            }
        }

    }
}
