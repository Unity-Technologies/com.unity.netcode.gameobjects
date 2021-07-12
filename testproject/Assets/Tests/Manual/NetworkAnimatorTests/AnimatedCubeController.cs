using MLAPI;
using UnityEngine;

namespace Tests.Manual.NetworkAnimatorTests
{
    [RequireComponent(typeof(Animator))]
    public class AnimatedCubeController : NetworkBehaviour
    {
        private Animator m_Animator;
        private bool m_Rotate;
    
        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_Rotate = m_Animator.GetBool("Rotate");
        }

        private void Update()
        {
            if (IsOwner)
            {
                if(Input.GetKeyDown(KeyCode.C))
                {
                    m_Rotate = !m_Rotate;
                    m_Animator.SetBool("Rotate", m_Rotate);
                }  
                if(Input.GetKeyDown(KeyCode.Space))
                {
                    m_Animator.SetTrigger("Pulse");
                }  
            }
        }
    }
}
