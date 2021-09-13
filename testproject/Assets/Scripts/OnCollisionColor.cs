using System.Collections;
using UnityEngine;

public class OnCollisionColor : MonoBehaviour
{
    private int m_CollisionCount;

    public void OnCollisionEnter(Collision other)
    {
        m_CollisionCount++;
        HandleCollisionCountChanged();
        StartCoroutine(StayColored());
    }

    private void HandleCollisionCountChanged()
    {
        if (m_CollisionCount > 0)
        {
            GetComponent<Renderer>().material.color = Color.red;
        }
        else
        {
            GetComponent<Renderer>().material.color = Color.white;
        }
    }

    private IEnumerator StayColored()
    {
        yield return new WaitForSeconds(0.2f);
        m_CollisionCount--;
        HandleCollisionCountChanged();
    }
}
