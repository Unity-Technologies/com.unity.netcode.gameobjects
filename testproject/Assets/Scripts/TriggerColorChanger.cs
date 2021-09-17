using UnityEngine;

public class TriggerColorChanger : MonoBehaviour
{
    private Material m_Material;

    private int m_ActiveTriggerCount;

    private void Start()
    {
        m_Material = GetComponent<Renderer>().material;
    }

    private void OnTriggerEnter(Collider other)
    {
        m_Material.color = Color.green;
        m_ActiveTriggerCount++;
    }

    private void OnTriggerExit(Collider other)
    {
        m_ActiveTriggerCount--;
        if (m_ActiveTriggerCount == 0)
        {
            m_Material.color = Color.white;
        }
    }
}
