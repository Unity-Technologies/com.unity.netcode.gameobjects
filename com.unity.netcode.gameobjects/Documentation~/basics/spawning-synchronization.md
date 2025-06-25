# Spawning synchronization

Ensuring that objects spawn in a synchronized manner across clients can be difficult, although the challenges differ depending on which [network topology](../terms-concepts/network-topologies.md) you're using.

## Spawning synchronization in client-server

Due to the restrictive nature of only the server having the authority to spawn objects, visual anomalies between clients are common in [client-server](../terms-concepts/client-server.md) contexts. This typically occurs when using an owner-authoritative motion model, also known as a ClientNetworkTransform, and trying to visually synchronize the spawning of an object relative to a local player's current position (for example, a player shooting a projectile).

![Client server spawn synchronization](../images/client-server-spawn-sync.jpg)

In the diagram above, a client-owned object (a player) is in motion while firing a projectile. The code implementing this behavior involves sending a message (typically an [RPC](../advanced-topics/messaging-system.md)) from the client to the server to get an object to spawn based on the player's input. Due to the latency involved in sending this message to the server and then propagating the resulting spawn message across all clients, including back to the initiating client, the spawn projectile message reaches the initiating client after the client's local player has moved, resulting in a visual disconnect between the local player and the projectile.

These types of issues are commonly resolved using a client prediction system or by separating the visual representation of the projectile from the NetworkObject and writing a script that moves the visual representation in the desired direction of the yet-to-be-spawned NetworkObject and then synchronizing the two over time once the NetworkObject is spawned. Both approaches are complex to implement in a client-server topology.

## Examples

This client-server example script requires splitting the logic between client and server. The client has to send a message to the server in order to spawn the projectile, which introduces the latency previously described between a player's motion and the projectile being spawned.

### Client-server

```
/// <summary>
/// Client-Server Spawning
/// Pseudo player weapon that is assumed to be
/// attached to a fixed child node of the player.
/// (i.e. like a hand node or the like)
/// </summary>
public class PlayerWeapon : NetworkBehaviour
{
    // For example purposes, the projectile to spawn
    public GameObject Projectile;


    // For example purposes, an offset from the weapon
    // to spawn the projectile.
    public GameObject WeaponFiringOffset;


    public void FireWeapon()
    {
        if (!IsOwner)
        {
            return;
        }


        if (IsServer)
        {
            OnFireWeapon();
        }
        else
        {
            OnFireWeaponServerRpc();
        }
    }


    [ServerRpc]
    private void OnFireWeaponServerRpc()
    {
        OnFireWeapon();
    }


    private void OnFireWeapon()
    {
        var instance = Instantiate(Projectile);
        var instanceNetworkObject = instance.GetComponent<NetworkObject>();
        instance.transform.position = WeaponFiringOffset.transform.position;
        instance.transform.forward = transform.forward;
        instanceNetworkObject.Spawn();
    }
}
```
