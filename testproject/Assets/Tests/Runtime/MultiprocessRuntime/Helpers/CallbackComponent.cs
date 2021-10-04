using System;
using UnityEngine;

/// <summary>
/// Component who's purpose is to expose callbacks to code tests
/// </summary>
public class CallbackComponent : MonoBehaviour
{
    public Action<float> OnUpdate;

    // Update is called once per frame
    private void Update()
    {
        try
        {
            OnUpdate?.Invoke(Time.deltaTime);
        }
        catch (Exception e)
        {
            TestCoordinator.Instance.WriteErrorServerRpc(e.ToString());
            throw;
        }
    }
}
