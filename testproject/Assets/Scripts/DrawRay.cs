using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawRay : MonoBehaviour
{
    public float RayLength = 40;

    private Transform m_Transform;
    private LineRenderer m_LineRenderer;

    private void Awake()
    {
        TryGetComponent(out m_Transform);
        TryGetComponent(out m_LineRenderer);
        m_LineRenderer.SetPosition(0, transform.position);
        m_LineRenderer.SetPosition(1, transform.position + transform.forward * RayLength);
    }

    private void FixedUpdate()
    {
        var ray = new Ray(m_Transform.position, m_Transform.forward * RayLength);

        var point = Physics.Raycast(ray, out var hit, RayLength, Physics.DefaultRaycastLayers)
            ? hit.point
            : m_Transform.position + m_Transform.forward * RayLength;
        if (hit.collider != null && hit.collider.tag != "Floor" && hit.collider.tag != "Boundary")
        {
            m_LineRenderer.startColor = Color.red;
            m_LineRenderer.endColor = Color.red;
        }
        else
        {
            m_LineRenderer.startColor = Color.white;
            m_LineRenderer.endColor = Color.white;
        }
        m_LineRenderer.SetPosition(0, transform.position);
        m_LineRenderer.SetPosition(1, point);
    }
}
