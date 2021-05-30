using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using NUnit.Framework.Internal.Commands;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component who's purpose is to expose callbacks to code tests
/// </summary>
public class CallbackComponent : MonoBehaviour
{
    public Action OnStart;
    public Action<float> OnUpdate;

    // Start is called before the first frame update
    private void Start()
    {
        OnStart?.Invoke();
    }

    // Update is called once per frame
    private void Update()
    {
        OnUpdate?.Invoke(Time.deltaTime);
    }
}
