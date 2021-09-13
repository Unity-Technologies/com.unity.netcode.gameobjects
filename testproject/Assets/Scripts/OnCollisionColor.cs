using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnCollisionColor : MonoBehaviour
{
    private int CollisionCount;

    public void OnCollisionEnter(Collision other)
    {
        CollisionCount++;
        HandleCollisionCountChanged();
        StartCoroutine(StayColored());
    }

    private void HandleCollisionCountChanged()
    {
        if (CollisionCount > 0)
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
        CollisionCount--;
        HandleCollisionCountChanged();
    }
}
