# Understanding ownership and authority

By default, Netcode for GameObjects assumes a [client-server topology](../terms-concepts/client-server.md), in which the server owns all NetworkObjects (with [some exceptions](networkobject.md#ownership)) and has ultimate authority over [spawning and despawning](object-spawning.md).

## Checking for authority

### `IsServer`

`IsServer` or `!IsServer` is the traditional client-server method of checking whether the current context has authority.

```
public class MonsterAI : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }
        // Server-side monster init script here
        base.OnNetworkSpawn();
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer)
        {
            return;
        }
        // Server-side updates monster AI here
    }
}
```
