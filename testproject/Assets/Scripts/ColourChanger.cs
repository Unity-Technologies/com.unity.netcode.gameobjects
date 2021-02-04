using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColourChanger : MonoBehaviour
{
  private Renderer render;
  private Color prevColour;

  void Start()
  {
    render = GetComponent<Renderer>();
  }
  
  void OnCollisionEnter(Collision collision)
  {
    prevColour = render.material.color;
    render.material.color = Color.green;
  }

  void OnCollisionExit()
  {
    render.material.color = prevColour;
  }

  void Update()
  {
    // Reset Colour after 2 seconds
    if (render.material.color != Color.white)
    {
      StartCoroutine(ResetColour(2));
    }
  }

  IEnumerator ResetColour(float waitTime)
  {
    yield return new WaitForSeconds(waitTime);

    render.material.color = Color.white;
  }
}
