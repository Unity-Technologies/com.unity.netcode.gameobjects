using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace Tests.Manual.NetworkAnimatorTests
{
    public class AnimationUserInput : NetworkBehaviour
    {
        private List<AnimatedCubeController> m_Controllers = new List<AnimatedCubeController>();
        // Start is called before the first frame update
        private void Start()
        {
            m_Controllers = GetComponentsInChildren<AnimatedCubeController>().ToList();
        }

        // Update is called once per frame
        private void LateUpdate()
        {
            if (IsServer && IsSpawned && NetworkManager.ConnectedClientsIds.Count - (NetworkManager.IsHost ? 1 : 0) > 0)
            {
                var clientIdCount = NetworkManager.ConnectedClientsIds.Count;


                if (Input.GetKeyDown(KeyCode.S))
                {
                    foreach (var controller in m_Controllers)
                    {
                        if (controller.IsServerAuthority())
                        {
                            continue;
                        }
                        var newOwnerId = NetworkManager.ServerClientId;
                        var index = NetworkManager.ConnectedClientsIds.ToList().IndexOf(controller.OwnerClientId);
                        index++;
                        if (IsHost)
                        {
                            index = index % NetworkManager.ConnectedClientsIds.Count;
                        }

                        if (index > clientIdCount)
                        {
                            index = 0;
                        }
                        else
                        {
                            newOwnerId = NetworkManager.ConnectedClientsIds[index];
                        }

                        controller.NetworkObject.ChangeOwnership(newOwnerId);
                    }
                }
            }

            if (!IsSpawned || !IsOwner)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                foreach (var controller in m_Controllers)
                {
                    controller.ToggleRotateAnimation();
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                foreach (var controller in m_Controllers)
                {
                    controller.PlayPulseAnimation();
                }
            }
        }
    }
}
