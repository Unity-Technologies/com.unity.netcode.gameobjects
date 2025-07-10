# NetworkObject and NetworkBehaviour

`GameObjects`, `NetworkObjects` and `NetworkBehaviour` aren't serializable types so they can't be used in `RPCs` or `NetworkVariables` by default.

There are two convenience wrappers which can be used to send a reference to a `NetworkObject` or a `NetworkBehaviour` over RPCs or `NetworkVariables`.

## NetworkObjectReference

`NetworkObjectReference` can be used to serialize a reference to a `NetworkObject`. It can only be used on already spawned `NetworkObjects`.

Here is an example of using `NetworkObject` reference to send a target `NetworkObject` over an RPC:
```csharp
public class Weapon : NetworkBehaviour
{
    public void ShootTarget(GameObject target)
    {
        var targetObject = target.GetComponent<NetworkObject>();
        ShootTargetServerRpc(targetObject);
    }

    [Rpc(SendTo.Server)]
    public void ShootTargetServerRpc(NetworkObjectReference target)
    {
        if (target.TryGet(out NetworkObject targetObject))
        {
            // deal damage or something to target object.
        }
        else
        {
            // Target not found on server, likely because it already has been destroyed/despawned.
        }
    }
}
```

### Implicit Operators

There are also implicit operators which convert from/to `NetworkObject/GameObject` which can be used to simplify code. For instance the above example can also be written in the following way:
```csharp
public class Weapon : NetworkBehaviour
{
    public void ShootTarget(GameObject target)
    {
        ShootTargetServerRpc(target);
    }

    [Rpc(SendTo.Server)]
    public void ShootTargetServerRpc(NetworkObjectReference target)
    {
        NetworkObject targetObject = target;
    }
}
```
> [!NOTE]
> The implicit conversion to `NetworkObject` / `GameObject` will result in `Null` if the reference can't be found.

## NetworkBehaviourReference

`NetworkBehaviourReference` works similar to `NetworkObjectReference` but is used to reference a specific `NetworkBehaviour` component on a spawned `NetworkObject`.

```cs
public class Health : NetworkBehaviour
{
    public NetworkVariable<int> Health = new NetworkVariable<int>();
}

public class Weapon : NetworkBehaviour
{
    public void ShootTarget(GameObject target)
    {
        var health = target.GetComponent<Health>();
        ShootTargetServerRpc(health, 10);
    }

    [Rpc(SendTo.Server)]
    public void ShootTargetServerRpc(NetworkBehaviourReference health, int damage)
    {
        if (health.TryGet(out Health healthComponent))
        {
            healthComponent.Health.Value -= damage;
        }
    }
}
```

## How NetworkObjectReference & NetworkBehaviourReference work

`NetworkObjectReference` and `NetworkBehaviourReference` are convenience wrappers which serialize the id of a `NetworkObject` when being sent and on the receiving end retrieve the corresponding ` ` with that id. `NetworkBehaviourReference` sends an additional index which is used to find the right `NetworkBehaviour` on the `NetworkObject`.

Both of them are structs implementing the `INetworkSerializable` interface.
