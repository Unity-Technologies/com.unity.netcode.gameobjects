using MLAPI;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private float m_Speed = 20.0f;
    private float m_RotSpeed = 5.0f;
    private Rigidbody m_Rigidbody;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        var temp = transform.position;
        temp.y = 0.5f;
        transform.position = temp;

        if (m_Rigidbody)
        {
            // Only the owner should ever move an object
            // If we don't set the non-local-player object as kinematic,
            // the local physics would apply and result in unwanted position
            // updates being sent up
            m_Rigidbody.isKinematic = !IsLocalPlayer;
        }
    }

    private void FixedUpdate()
    {
        if (IsLocalPlayer)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                transform.position += Time.fixedDeltaTime * m_Speed * transform.forward;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                transform.position -= Time.fixedDeltaTime * m_Speed * transform.forward;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                Quaternion rot = Quaternion.Euler(0, 90 * m_RotSpeed * Time.fixedDeltaTime, 0);
                transform.rotation = rot * transform.rotation;
            }

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                Quaternion rot = Quaternion.Euler(0, -90 * m_RotSpeed * Time.fixedDeltaTime, 0);
                transform.rotation = rot * transform.rotation;
            }
        }
    }
}
