using System.Collections;
using UnityEngine;

public class OnCollisionColor : MonoBehaviour
{
    private Material m_Material;
    private int m_CollisionCount;
    
    private void Start()
    {
        m_Material = GetComponent<Renderer>().material;
    }

    public void OnCollisionEnter(Collision other)
    {
        m_CollisionCount++;
        HandleCollisionCountChanged();
        StartCoroutine(StayColored());
    }

    private void HandleCollisionCountChanged()
    {
        m_Material.color = m_CollisionCount > 0 ? Color.red : Color.white;
    }

    private IEnumerator StayColored()
    {
        yield return new WaitForSeconds(0.2f);
        m_CollisionCount--;
        HandleCollisionCountChanged();
    }
}
