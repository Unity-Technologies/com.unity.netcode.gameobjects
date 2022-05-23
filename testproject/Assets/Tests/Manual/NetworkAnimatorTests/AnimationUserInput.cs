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

            if (Input.GetKeyDown(KeyCode.Return))
            {
                foreach (var controller in m_Controllers)
                {
                    controller.PlayPulseAnimation(false);
                }
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                foreach (var controller in m_Controllers)
                {
                    controller.TestAnimator();
                }
            }
        }
    }
}
