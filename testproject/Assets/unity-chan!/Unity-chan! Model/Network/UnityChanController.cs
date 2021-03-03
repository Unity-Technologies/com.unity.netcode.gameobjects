using MLAPI;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class UnityChanController : NetworkBehaviour
{
    private Animator m_Animator;

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (IsOwner)
        {
            m_Animator.SetBool("Happy", Input.GetKey(KeyCode.C));
        }
    }
}