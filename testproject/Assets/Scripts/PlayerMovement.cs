using MLAPI;
using UnityEngine;

public class PlayerMovement : NetworkedBehaviour
{
    private float speed = 20.0f;
    private float rotSpeed = 5.0f;

    private Rigidbody m_Rigidbody;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        var temp = transform.position;
        temp.y = 0.5f;
        transform.position = temp;

        if (m_Rigidbody)
        {
            m_Rigidbody.isKinematic = !IsLocalPlayer;
        }
    }

    private void FixedUpdate()
    {
        if (IsLocalPlayer)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                transform.position += Time.fixedDeltaTime * speed * transform.forward;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                transform.position -= Time.fixedDeltaTime * speed * transform.forward;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                Quaternion rot = Quaternion.Euler(0, 90 * rotSpeed * Time.fixedDeltaTime, 0);

                transform.rotation = rot * transform.rotation;
//                m_direction = rot * m_direction;
            }

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                Quaternion rot = Quaternion.Euler(0, -90 * rotSpeed * Time.fixedDeltaTime, 0);

                transform.rotation = rot * transform.rotation;
//                m_direction = rot * m_direction;
            }
        }
    }
}
