using System;
using UnityEngine;

namespace MLAPI.RuntimeTests
{
    public class CoroutineRunner : MonoBehaviour
    {
        void OnDestroy()
        {
            Debug.Log("Coroutine runner destroyed: " + name);
        }
    }
}
