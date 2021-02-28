using MLAPI;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ChickController : NetworkBehaviour
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
            m_Animator.SetBool("Eat", Input.GetKey(KeyCode.C));
        }
    }
}