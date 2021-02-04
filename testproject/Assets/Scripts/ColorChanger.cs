using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorChanger : MonoBehaviour
{
  private Renderer render;
  private Color prevColor;

  void Start()
  {
    render = GetComponent<Renderer>();
  }

  void OnCollisionEnter(Collision collision)
  {
    prevColor = render.material.color;
    render.material.color = Color.green;
  }

  void OnCollisionExit()
  {
    render.material.color = prevColor;
  }

  void Update()
  {
    // Reset Color after 2 seconds
    if (render.material.color != Color.white)
    {
      StartCoroutine(ResetColor(2));
    }
  }

  IEnumerator ResetColor(float waitTime)
  {
    yield return new WaitForSeconds(waitTime);

    render.material.color = Color.white;
  }
}
