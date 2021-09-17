using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawRay : MonoBehaviour
{
    private LineRenderer m_LineRenderer;

    private void Awake()
    {
        m_LineRenderer = GetComponent<LineRenderer>();
        m_LineRenderer.SetPosition(0, transform.position);
    }

    private void FixedUpdate()
    {
        if (Physics.Raycast(new Ray(transform.position, transform.forward * 10), out RaycastHit hit, 10, Physics.DefaultRaycastLayers))
        {
            m_LineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            m_LineRenderer.SetPosition(1, transform.position + transform.forward * 10);
        }
    }
}
