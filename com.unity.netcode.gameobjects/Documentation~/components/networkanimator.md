# NetworkAnimator

The `NetworkAnimator` component provides you with a fundamental example of how to synchronize animations during a network session. Animation states are synchronized with players joining an existing network session and any client already connected before the animation state changing.

* Players joining an existing network session will be synchronized with:
    * All the `Animator`'s current properties and states.
        * *With exception to `Animator` trigger properties. These are only synchronized with already connected clients.*
    * Any in progress transition
* Players already connected will be synchronized with changes to `Animator`:
    * States
    * Transitions
    * Properties
        * `NetworkAnimator` will only synchronize properties that have changed since the earlier frame property values.
        * Since triggers are similar to an "event," when an `Animator` property is set to `true` it will always be synchronized.

`NetworkAnimator` can operate in two authoritative modes:

* Server Authoritative (default): Server initiates animation state changes.
    * Owner's can still invoke `NetworkAnimator.SetTrigger`.
* Client Authoritative: Client owners start animation state changes.


> [!NOTE]
> You need to use `Unity.Netcode.Components` to reference components such as `NetworkAnimator`.

## Animator Trigger Property

The `Animator` trigger property type ("trigger") is basically nothing more than a Boolean value that, when set to `true`, will get automatically reset back to `false` after the `Animator` component has processed the trigger. Usually, a trigger is used to start a transition between `Animator` layer states. In this sense, one can think of a trigger as a way to signal the "beginning of an event." Because trigger properties have this unique behavior, they **require** that you to set the trigger value via the `NetworkAnimator.SetTrigger` method.

> [!NOTE]
> If you set a trigger property using `Animator.SetTrigger` then this won't be synchronized with non-owner clients.

## Server Authoritative Mode

The default setting for `NetworkAnimator` is server authoritative mode. When operating in server authoritative mode, any animation state changes that are set (triggers) or detected (change in layer, state, or any `Animator` properties excluding triggers) on the server side will be synchronized with all clients. Because the server initiates any synchronization of changes to an `Animator` 's state, a client that's the owner of the `NetworkObject` associated with the `NetworkAnimator` can lag by roughly the full round trip time (RTT). Below is a timing diagram to show this:

![ServerAuthMode](../images/NetworkAnimatorServerAuthTiming.png)

In the above diagram, a client might be sending the server an RPC to tell the server that the player is performing some kind of action that can change the player's animations (including setting a trigger). Under this scenario, the client sends an RPC to the server (half RTT), the server processes the RPC, the associated `Animator` state changes are detected by the `NetworkAnimator` (server-side), and then all clients (including the owner client) are synchronized with the changed.

**Server authoritative model benefits:**

* If running a plain server (non-host), this model helps reduce the synchronization latency between all client animations.

**Server authoritative model drawbacks:**

* Hosts will always be "slightly ahead" of all other clients which may or may not be an issue for your project.
* Client owners will experience a latency between performing an action (moving, picking something up, anything that causes an `Animator` state change).

## Owner Authoritative Mode

Usually, your project's design (or personal preference) might require that owners are immediately updated to any `Animator` state changes. The most typical reason would be to give the local player with instantaneous visual (animation) feedback. To create an owner authoritative `NetworkAnimator` you need to create a new class that's derived from `NetworkAnimator`, override the `NetworkAnimator.OnIsServerAuthoritative` method, and within the overridden `OnIsServerAuthoritative` method you should return false like in the example provided below:

```csharp
    public class OwnerNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
```

Looking at the timing for an owner authoritative `NetworkAnimator`, in the diagram below, you can see that while the owner client gets "immediate visual animation response" the non-owner clients end up being roughly one full RTT behind the owner client and a host would be half RTT behind the owner client.

![ServerAuthMode](../images/NetworkAnimatorOwnerAuthTiming.png)

In the above diagram, it shows that the owner client has an `Animator` state change that's detected by the `NetworkAnimator` ( `OwnerNetworkAnimator`) which automatically synchronizes the server with the changed state. The server applies the change(s) locally and then broadcasts this state change to all non-owner clients.

**Owner authoritative model benefits:**

* The owner is provided instant visual feedback of `Animator` state changes, which does offer a smoother experience for the local player.

**Owner authoritative model drawbacks:**

* Non-owner clients lag behind the owner client's animation by roughly one full RTT.
* A host lags behind the owner client's animation by roughly half RTT.

> [!NOTE]
> The same rule for setting trigger properties still applies to owner clients. As such, if you want to programmatically set a trigger then you still need to use `NetworkAnimator.SetTrigger`.

## Using NetworkAnimator

Using `NetworkAnimator` is a pretty straight forward approach with the only subtle difference being whether you are using a server or owner authoritative model.

> [!NOTE]
> `NetworkAnimator` is one of several possible ways to synchronize animations during a network session. Netcode for GameObjects provides you with the building blocks (RPCs, NetworkVariables, and Custom Messages) needed to create a completely unique animation synchronization system that has a completely different and potentially more optimized approach. `NetworkAnimator` is a straight forward approach provided for users already familiar with the `Animator` component and, depending upon your project's design requirements, might be all that you need.

### Server Authoritative

If you decide you want to use the server authoritative model, then you can add a `NetworkAnimator` component to either the same `GameObject` that has the `NetworkObject` component attached to it or any child `GameObject`. In the below screenshot, you can see a network Prefab that houses two authoritative models. The `NetworkAnimatorCube-Server` `GameObject` has an `Animator` component, an `AnimatedCubeController` component (used for manual testing), and the `NetworkAnimator` component that has a reference to the `Animator` component.

![Usage-1](../images/NetworkAnimatorUsage-1.png)

### Owner Authoritative

If you decide you want to use the owner authoritative model, then (for example purposes) you would use your derived `OwnerNetworkAnimator` component as opposed to the default `NetworkAnimator` component like in the screenshot below:

![Usage-1](../images/NetworkAnimatorUsage-2.png)

> [!NOTE]
> While it isn't advised to have different `NetworkAnimator` authoritative models "under the same root network Prefab `GameObject`, " you can have multiple children that each have their own `Animator` and `NetworkAnimator` all housed under a single `NetworkObject` and all use the same authoritative model. However, you should always consider the balance between performance (CPU or bandwidth consumption) and convenience/modularity.

### Changing Animator Properties

For all `Animator` properties (except for triggers), you can set them directly via the `Animator` class. As an example, you might use the player's normalized velocity as a way to control the walking or running animation of a player. You might have an `Animator` `float` property called "AppliedMotion" that you would set on the authoritative instance (server or owner) like such:

```csharp
public void ApplyMotion(Vector3 playerVelocity)
{
    m_Animator.SetFloat("AppliedMotion", playerVelocity.normalized.magnitude);
}
```

For triggers you always want to use `NetworkAnimator`. One example might be that you use a trigger, called it " `IsJumping`, " to start a blended transition between the player's walking/running animation and the jumping animation:

```csharp
public void SetPlayerJumping(bool isJumping)
{
    m_NetworkAnimator.SetTrigger("IsJumping");
}
```

> [!NOTE] Changing meshes
> When swapping a skinned mesh with another reparented skinned mesh, you can invoke the `Rebind ` method on the `Animator` components: `Animator.Rebind()`.
