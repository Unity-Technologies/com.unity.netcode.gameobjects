using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawRay : MonoBehaviour
{
    private const float k_RayLength = 10;

    private Transform m_Transform;
    private LineRenderer m_LineRenderer;

    private void Awake()
    {
        TryGetComponent(out m_Transform);
        TryGetComponent(out m_LineRenderer);
        m_LineRenderer.SetPosition(0, transform.position);
    }

    private void FixedUpdate()
    {
        var ray = new Ray(m_Transform.position, m_Transform.forward * k_RayLength);

        var point = Physics.Raycast(ray, out var hit, k_RayLength, Physics.DefaultRaycastLayers)
            ? hit.point
            : m_Transform.position + m_Transform.forward * k_RayLength;

        m_LineRenderer.SetPosition(1, point);
    }
}
