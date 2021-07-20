using MLAPI;
using UnityEngine;

public class MoveInCircle : MonoBehaviour
{
    [SerializeField]
    private float m_MoveSpeed = 5;

    [SerializeField]
    private float m_RotationSpeed = 30;

    void Update()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            transform.position = transform.position + m_MoveSpeed * transform.forward * Time.deltaTime;
            transform.Rotate(0, m_RotationSpeed * Time.deltaTime, 0);
        }
    }
}
