# Deferred despawning

> [!NOTE]
> Deferred despawning is only available for games using a [distributed authority topology](../terms-concepts/distributed-authority.md).

In addition to the visual latency issues described on the [spawning synchronization page](spawning-synchronization.md), networked games also experience latency issues when despawning objects. In [client-server](../terms-concepts/client-server.md) contexts, these problems are typically addressed using client prediction models, but in a distributed authority context there is a simpler alternative solution: deferred despawning.

## Why deferred despawning

Issues with despawning occur when objects are despawned too early. The client that owns the projectile detects a collision, triggers any local animations, and despawns the projectile locally. For the player being hit, they see the projectile despawn before it reaches them and animations can appear to trigger at the wrong point in time. This can be seen in the diagram below.

![Without deferred despawning](../images/without-deferred-despawning.jpg)

Deferred despawning allows you to add a tick-based delay to a NetworkObject prior to despawning it locally. The deferred despawn tick value is then added to the `DestroyObjectMessage` so that other clients, upon receiving the message, defer processing the destroy message until the specified tick value has been reached. The results of this deferred despawn adjustment can be seen in the diagram below.

![With deferred despawning](../images/with-deferred-despawning.jpg)

Deferred despawning allows non-owner instances of an object to complete their interpolation towards the last transform state update before despawning the object. Even though non-owner clients can be visually latent by several network ticks behind the authority, all clients end up with the same visual experience since NetworkTransform deltas are tick synchronized.

## Implement deferred despawning

Implementing deferred despawning is a three-step process, as described in the diagram below.

![Implementing deferred despawning](../images/implementing-deferred-despawning.jpg)

The process above is broken down into two steps that are performed on the authority instance, and one step performed on the non-authority instance(s).

|Authority instance|Non-authority instance(s)|
|---|---|
|1. Invoke the `NetworkObject.DeferDespawn` method while providing the number of ticks to offset despawning by on non-authority instance(s), relative to the local client's current known network tick.<br/> 2. If overridden, handle any changes to state (i.e. NetworkVariables) when `NetworkBehaviour.OnDeferringDespawn` is invoked.|1. Use any updated states to synchronize the visual portion of a deferred despawn (for example, starting a particle system to represent the point of impact).|

### Deferred despawning example

Below is a basic example demonstrating how deferred despawning can be used to visually synchronize a projectile's impact with explosion effects:

```
public class ExplodingProjectile : NetworkBehaviour
{
    // The explosion network prefab
    public GameObject ExplosionPrefab;
    public int DespawnTickOffset = 4;
    private ExplosionFx m_SpawnedExplosion;


    // Permission are always owner write and everyone read in distributed authority
    // Authority assigns this value after spawning the ExplosionFx within the overridden
    // OnDeferringDespawn method prior to the local ExplodingPrrojectile is despawned
    // locally.
    private NetworkVariable<NetworkBehaviourReference> m_ExplosionFx = new NetworkVariable<NetworkBehaviourReference>();


    public override void OnNetworkSpawn()
    {
        if (!HasAuthority)
        {
            m_ExplosionFx.OnValueChanged += OnExplosionFxChanged;
        }


        base.OnNetworkSpawn();
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (!IsSpawned || !HasAuthority)
        {
            return;
        }
        // Typically you would want to check what you hit
        // to make sure you want to "explode" the projectile
        // first. This example assumes that a check was performed.


        // Start with the projectile's position
        var explodePoint = transform.position;
        if (collision.contacts.Length > 0 )
        {
            // Example purposes, just use the first contact
            explodePoint = collision.contacts[0].point;
        }
        HandleExplosion(explodePoint);
    }


    private void HandleExplosion(Vector3 explodePoint)
    {
        // Example purposes only, we recommend using object pools
        // using an INetworkPrefabInstanceHandler implementation
        // and registering that with the NetworkManager.PrefabHandler.
        var instance = Instantiate(ExplosionPrefab);

        // position the explosion
        instance.transform.position = explodePoint;


        // Assign this for later use (example purposes)
        m_SpawnedExplosion = instance.GetComponent<ExplosionFx>();

        // Spawn the explosion
        var instanceObj = instance.GetComponent<NetworkObject>();
        instanceObj.Spawn();


        // Defer the despawning of the projectile instance
        NetworkObject.DeferDespawn(DespawnTickOffset);

        // The local instance should be despawned at this point
    }


    /// <summary>
    /// Invoked on the authority side when it is deferring the
    /// despawning of the NetworkObject.
    /// </summary>
    /// <remarks>
    /// This is a good time to set any NetworkVariable states as
    /// they will be sent to clients prior to the defer despawn message.
    /// </remarks>
    /// <param name="despawnTick">the final future network tick
    /// non-authority instances will despawn the clone instance.</param>
    public override void OnDeferringDespawn(int despawnTick)
    {
        // This lets the non-authority instances know what ExplosionFx instance is
        // associated with this NetworkObject.
        m_ExplosionFx = new NetworkVariable<NetworkBehaviourReference>(m_SpawnedExplosion);


        // Apply any other state updates here


        base.OnDeferringDespawn(despawnTick);
    }


    /// <summary>
    /// Non-authority registers for this and acquires the ExplosionFX
    /// of the network prefab spawned.
    /// </summary>
    private void OnExplosionFxChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
    {
        // If possible, get the ExplosionFx component
        current.TryGet(out m_SpawnedExplosion);
    }


    public override void OnNetworkDespawn()
    {
        // When non-authority instances finally despawn,
        // the explosion FX will begin playing.
        if (!HasAuthority)
        {
            if (m_SpawnedExplosion)
            {
                m_SpawnedExplosion.SetParticlePlayingState(true);
            }
            m_ExplosionFx.OnValueChanged -= OnExplosionFxChanged;
        }
        base.OnNetworkDespawn();
    }
}
```

The pseudo script above excludes the motion of the projectile, but makes the assumption that authority is moving the projectile and that when the projectile impacts a valid object, the `OnCollisionEnter` method will be invoked.

|Authority instance|Non-authority instance(s)|
|---|---|
|1. Move until collision (excluded from the above example)<br/> 2. Upon collision: instantiate ExplosionFX, position ExplosionFX, acquire the ExplosionFX component, spawn the ExplosionFX, defer despawning the projectile<br/> 3. When deferring despawning the projectile, assign the NetworkBehaviourReference to the ExplosionFX<br/> 4. Despawn locally (happens automatically at end of the DeferDespawn)|1. When spawned, register changes to the m_ExplosionFX NetworkVariable<br/> 2. Continue to interpolate towards last updated position (handled by NetworkTransform)<br/> 3. When m_ExplosionFX NetworkVariable changes, assign the local m_SpawnExplosion ExplosionFX component of the explosion associated with the current projectile instance (to be used when despawned)<br/> 4.When despawned, start the ExplosionFX particle system via m_SpawnExplosion|

The non-authority instance still spawns the ExplosionFX NetworkObject at the same relative time frame as the authority, however it doesn't start its particle system until the local non-authority projectile instance is despawned. Since the projectile has had its despawn deferred until a future network tick, the non-authority instance interpolates up to its last updated position from the authority side and then shortly (in milliseconds) after is despawned and the explosion particle system started.

> [!NOTES]
> The spawning explosion FX example above is only for example purposes and would be less bandwidth and processor intensive if you used a local particle FX pool that you pull from and began playing on both the authority and non-authority sides when the object in question is despawned. The same deferred despawn principles would be used without the need to spawn an additional object. However, providing the spawned explosion example also covers other scenarios where spawning is required.

For example purposes, the below script is all that you would need to control starting and stopping the explosion particle system:

```
public class ExplosionFx : NetworkBehaviour
{
    public ParticleSystem ParticleSystem;


    private void OnEnable()
    {
        // In the event a pool is used, it is always good to
        // make sure the particle system is not playing
        SetParticlePlayingState();
    }


    public override void OnNetworkSpawn()
    {
        // Authority starts the particle system (locally) when spawned
        if (HasAuthority)
        {
            SetParticlePlayingState(true);
        }
        base.OnNetworkSpawn();
    }


    /// <summary>
    /// Used to start/stop the particle system
    /// Non-Authority starts this when its associated projectile
    /// is finally despawned
    /// </summary>
    public void SetParticlePlayingState(bool isPlaying = false)
    {
        if (ParticleSystem)
        {
            if(isPlaying)
            {
                ParticleSystem.Play();
            }
            else
            {
                ParticleSystem.Stop();
            }
        }
    }
}
```

Note that the authority automatically starts the particle system upon being spawned. Non-authority instances are started by their paired projectile when despawned at the end of the deferred despawn defined network tick.
