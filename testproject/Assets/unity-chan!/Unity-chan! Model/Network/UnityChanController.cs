using MLAPI;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class UnityChanController : NetworkBehaviour
{
    private Animator m_Animator;
    private bool m_CanAnimateAgain;

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
        m_CanAnimateAgain = true;
    }

    private void Update()
    {
        if (IsOwner)
        {
            if(m_CanAnimateAgain && Input.GetKey(KeyCode.C))
            {
                m_Animator.SetBool("Happy", m_CanAnimateAgain);
                m_CanAnimateAgain = false;
                StartCoroutine(WaitForEndOfAnimation());
            }            
        }
    }

    private IEnumerator WaitForEndOfAnimation()
    {
        yield return new WaitForSeconds(m_Animator.GetCurrentAnimatorClipInfo(0)[0].clip.length);
        m_Animator.SetBool("Happy", m_CanAnimateAgain);
        m_CanAnimateAgain = true;
        yield return null;
    }
}
