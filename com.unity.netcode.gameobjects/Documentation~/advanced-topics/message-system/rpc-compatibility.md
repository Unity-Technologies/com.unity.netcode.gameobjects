# RPC migration and compatibility

This section provides information on compatibility and support for Unity Netcode for GameObjects (Netcode) features compared to previous Netcode versions.

## Cross-Compatibility

Learn more about standard RPC API's cross-compatibility only, not the framework as a whole. A method decorated with an RPC attribute will be statically registered with its assembly-scoped method signature hash.

A typical assembly-scoped method signature sample:

```
Game.dll / System.Void Shooter::PingRpc(System.Int32)
```

where:

- `Game.dll` is the Assembly
- `/` is a Separator
- `System.Void Shooter::PingRpc(System.Int32)` is the Method signature:

    - `System.Void` is the Return type
    - `Shooter` is the Enclosing type
    - `::` is the Scope resolution operator
    - `PingRpc` is the Method name
    - `(System.Int32)` is the Parameter type (no param names)

An RPC signature will be turned into a 32-bit integer using [xxHash](https://cyan4973.github.io/xxHash/) (XXH32) non-cryptographic hash algorithm.

The RPC signature hash changes when any of the following variables change:
* Assembly.
* Enclosing type.
* Method name.
* Method parameter type.

However, the RPC signature hash doesn't change when the names of the parameters for an existing RPC are the only variables that change.

When the RPC signature changes, it directs to a different invocation code path that has different serialization code. This means that the RPC method with the new signature doesn't invoke previous versions of that RPC method (for example, an RPC method from an older build).

| Compatibility | <i class="fp-check"></i> | Description |
| -- | :--: | -- |
| Cross-Build Compatibility | <i class="fp-check"></i> | As long as the RPC method signature is kept the same, it will be compatible between different builds. |
| Cross-Version Compatibility | <i class="fp-check"></i> | As long as the RPC method signature is kept the same, it will be compatible between different versions. |
| Cross-Project Compatibility | <i class="fp-x"></i> | The exact same RPC method signature can be defined in different projects. This is because the project name or project-specific token isn't part of RPC signature. Cross-project RPC methods aren't compatible with each other. |

## Deprecation of return values

Netcode supports RPC return values on convenience RPCs.

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

To achieve similar functionality, use the following:

```csharp
void MyRpcInvoker()
{
    MyRpcWithReturnValueRequestServerRpc(Random.Range(0f, 100f), Random.Range(0f, 100f));
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

import useBaseUrl from '@docusaurus/useBaseUrl';
import Link from '@docusaurus/Link';
