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
            if (!IsSpawned)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                foreach (var controller in m_Controllers)
                {
                    if ((controller.IsServerAuthoritative && (IsServer || IsOwner)) || (!controller.IsServerAuthoritative && IsOwner))
                    {
                        controller.ToggleRotateAnimation();
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                foreach (var controller in m_Controllers)
                {
                    if ((controller.IsServerAuthoritative && (IsServer || IsOwner)) || (!controller.IsServerAuthoritative && IsOwner))
                    {
                        controller.PlayPulseAnimation();
                    }
                }
            }
        }
    }
}
