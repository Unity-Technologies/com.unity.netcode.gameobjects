using System.Collections;
using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    private Material m_Material;
    private Color m_PrevColor;

    private void Start()
    {
        m_Material = GetComponent<Renderer>().material;
    }

    private void OnCollisionEnter(Collision collision)
    {
        m_PrevColor = m_Material.color;
        m_Material.color = Color.green;
    }

    private void OnCollisionExit()
    {
        m_Material.color = m_PrevColor;
    }

    private void Update()
    {
        // Reset Color after 2 seconds
        if (m_Material.color != Color.white)
        {
            StartCoroutine(ResetColor(2));
        }
    }

    private IEnumerator ResetColor(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        m_Material.color = Color.white;
    }
}
