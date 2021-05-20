using MLAPI;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class UnityNetChanController : NetworkBehaviour
{
    private Animator m_Animator;
    private bool m_Happy;
    
    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
        m_Happy = m_Animator.GetBool("Happy");
    }

    private void Update()
    {
        if (IsOwner)
        {
            if(Input.GetKeyDown(KeyCode.C))
            {
                m_Happy = !m_Happy;
                m_Animator.SetBool("Happy", m_Happy);
            }  
            if(Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log($"Input: jump {Time.frameCount}");
                m_Animator.SetTrigger("Jump");
            }  
        }
    }
}
