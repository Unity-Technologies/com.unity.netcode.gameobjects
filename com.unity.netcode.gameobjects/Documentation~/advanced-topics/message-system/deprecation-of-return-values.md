# Deprecation of return values

Netcode for GameObjects used to support RPC return values on convenience RPCs.

Example:

```csharp
public IEnumerator MyRpcCoroutine()
{
    RpcResponse<float> response = InvokeServerRpc(MyRpcWithReturnValue, Random.Range(0f, 100f), Random.Range(0f, 100f));

    while (!response.IsDone)
    {
        yield return null;
    }

    Debug.LogFormat("The final result was {0}!", response.Value);
}

[ServerRPC]
public float MyRpcWithReturnValue(float x, float y)
{
    return x * y;
}

```

Netcode for GameObjects no longer supports this feature. To achieve the same functionality, use a combination of the ServerRpc and ClientRpc methods. The following code demonstrates this method:

```csharp
void MyRpcInvoker()
{
    MyRpcWithReturnValueRequestServerRpc(Random.Range(0f, 100f)), Random.Range(0f, 100f)));
}

[Rpc(SendTo.Server)]
void MyRpcWithReturnValueRequestServerRpc(float x, float y)
{
    MyRpcWithReturnValueResponseClientRpc(x * y);
}

[Rpc(SendTo.ClientsAndHost)]
void MyRpcWithReturnValueResponseClientRpc(float result)
{
    Debug.LogFormat("The final result was {0}!", result);
}
```
