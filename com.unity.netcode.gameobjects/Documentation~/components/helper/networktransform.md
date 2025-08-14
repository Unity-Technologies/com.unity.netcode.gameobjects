# NetworkTransform

[NetworkTransform](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.Components.NetworkTransform.html) is a concrete class that inherits from [NetworkBehaviour](../core/networkbehaviour.md) and synchronizes [Transform](https://docs.unity3d.com/Manual/class-Transform.html) properties across the network, ensuring that the position, rotation, and scale of a [GameObject](https://docs.unity3d.com/Manual/working-with-gameobjects.html) are replicated to other clients.

The synchronization of a GameObject's Transform is a key netcode task, and usually proceeds in the following order:

1. Determine which Transform axis have changed and need to be synchronized.
2. Serialize the changed values.
3. Send the serialized values as messages to all other connected clients.
4. Process the messages and deserialize the values.
5. Apply the changed values to the appropriate axis.

There are other considerations when synchronizing Transform values, however, such as:

- Who controls the synchronization: is it the client, server, distributed authority service, or some combination?
- How often should the values be synchronized?
- What logic should you use to determine when and which values need to be synchronized?
- For objects with one or more child objects, should you synchronize world or local space axis values?
- How can you optimize the bandwidth for a Transform update?

## Add a NetworkTransform component to a GameObject

Because a NetworkTransform component is derived from the NetworkBehaviour class, it has many of the [same requirements](../core/networkbehaviour.md). For example, when adding a NetworkTransform component to a GameObject, it should be added to the same or any hierarchy generation relative to the NetworkObject component.

In the following image both NetworkTransform and NetworkObject components are on the same GameObject:

![image](../../images/networktransform/SingleGeneration.png)

Alternatively, the parent GameObject can have multiple children where any child can have a NetworkTransform:

![image](../../images/networktransform/MultiGeneration.png)

Theoretically, you can have a NetworkTransform on every child object of a 100 leaf deep hierarchy. However, it's recommended to exercise caution with the amount of nested NetworkTransforms in a network prefab, particularly if there will be many instances of the network prefab.

> [!NOTE]
> Generally, as long as there's at least one [NetworkObject](../core/networkobject.md) at the same GameObject hierarchy level or above, you can attach a NetworkTransform component to a GameObject. You could have a single root-parent GameObject that has a NetworkObject component and under the root-parent several levels of nested child GameObjects that all have NetworkTransform components attached to them. Each child GameObject doesn't require a NetworkObject component in order for the respective NetworkTransform component to synchronize properly.

### Nesting NetworkTransforms

When nesting NetworkTransforms, you should first determine if there are any alternative approaches to handling child object motion (such as synchronizing through animations or using an algorithm that uses the synchronized network time) to ensure you're not consuming unnecessary processing time and bandwidth.

For example, if you use a [NetworkAnimator](networkanimator.md) component to synchronize animations (instead of nested NetworkTransforms), then you don't need to manage synchronizing each child node of the model because the animation system and NetworkAnimator handle it for you. Similarly, if you want a child object to orbit its parent object, you can use the server time ([`NetworkManager.ServerTime`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?sufolder=/api/Unity.Netcode.NetworkManager.html#Unity_Netcode_NetworkManager_ServerTime)) to feed into an algorithm that's used on all instances (local and remote). This removes the need to synchronize motion between child and parent because the algorithm is driven by a value that's already being synchronized on all clients.

> [!NOTE]
> Generally, unless you have some unique motion that needs to be applied to a child object and it can't be synchronized through an animation or an algorithm based on a synchronized value, then you shouldn't nest NetworkTransforms. However, nesting NetworkTransforms is more optimal then creating multiple individual single-node network prefabs with NetworkTransforms and parenting those once they're spawned.

## Configure a NetworkTransform

When you select a NetworkTransform component, there are the following properties in the inspector view:

![image](../../images/networktransform/NetworkTransformProperties.png)

### Property synchronization

Some NetworkTransform properties are automatically synchronized by the authoritative instance to all non-authoritative instances. It's important to note that when any synchronized property changes, the NetworkTransform is effectively teleported (all values re-synchronized and interpolation reset), which can cause a single frame delta in position, rotation, and scale (depending on what's being synchronized). Keep this in mind when making adjustments to NetworkTransform properties during runtime.

#### Properties that cause a full state update

The following are a list of NetworkTransform properties that will cause a full state update (effectively a teleport) when changed during runtime by the authority instance:

- [UseUnreliableDeltas](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.3/api/Unity.Netcode.Components.NetworkTransform.html#Unity_Netcode_Components_NetworkTransform_UseUnreliableDeltas)
- [InLocalSpace](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.3/api/Unity.Netcode.Components.NetworkTransform.html#Unity_Netcode_Components_NetworkTransform_InLocalSpace)
- [Interpolate](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.3/api/Unity.Netcode.Components.NetworkTransform.html#Unity_Netcode_Components_NetworkTransform_Interpolate)
- [SlerpPosition](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.3/api/Unity.Netcode.Components.NetworkTransform.html#Unity_Netcode_Components_NetworkTransform_SlerpPosition)
- [UseQuaternionSynchronization](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.3/api/Unity.Netcode.Components.NetworkTransform.html#Unity_Netcode_Components_NetworkTransform_UseQuaternionSynchronization)
- [UseQuaternionCompression](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.3/api/Unity.Netcode.Components.NetworkTransform.html#Unity_Netcode_Components_NetworkTransform_UseQuaternionCompression)
- [UseHalfFloatPrecision](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.3/api/Unity.Netcode.Components.NetworkTransform.html#Unity_Netcode_Components_NetworkTransform_UseHalfFloatPrecision)

The following NetworkTransform properties can cause a full state update when changed during runtime by the authority instance:

- __Use Unreliable Deltas__
- __Interpolate__
    - __Slerp Position__
- __Use Quaternion Synchronization__
- __Use Quaternion Compression__
- __Use Half Float Precision__

### Axis to Synchronize

![image](../../images/networktransform/AxisToSynchronize.png)

You often don't need to synchronize all Transform values of a GameObject over the network. For example, if the scale of the GameObject never changes, you can deactivate it in the __Scale__ row of the __Axis to Synchronize__ area within the Inspector. Deactivating synchronization saves some processing costs and reduces network bandwidth consumption.

The term synchronizing refers to the synchronization of axis values over time. This is not to be confused with the initial synchronization of a Transform's values.

#### NetworkTransform vs NetworkObject Transform synchronization

You can have [nested NetworkTransforms](#nesting-networktransforms) as long as they're on the same GameObject that a NetworkObject is attached to, or on a child GameObject nested under a GameObject that a NetworkObject is attached to. Enable a NetworkObject's __Synchronize Transform__ property to synchronize the associated GameObject's Transform during initial synchronization. This only applies to that GameObject's Transform, which means that you need to synchronize the Transforms of any nested NetworkTransforms independently.

If you don't plan on changing the Transform's scale after the initial synchronization (that occurs when joining a network session or when a network prefab instance is spawned for the first time), then disabling the X, Y, and Z properties for __Scale__ synchronization removes some additional processing overhead per instance. However, if you have a nested NetworkTransform and want to apply a unique scale (per instance) that's applied to that nested NetworkTransform's Transform when it's first spawned, then the adjusted scale won't be synchronized.

Fortunately, the authority of the NetworkTransform instance can make changes to any of the __Axis to Synchronize__ properties during runtime and non-authoritative instances will receive updates for the axis marked for synchronization.

When disabling an axis to be synchronized for performance purposes, you should always consider that NetworkTransform won't send updates as long as the axis in question doesn't have a change that exceeds its [threshold](#thresholds) value. So, taking the scale example into consideration, it can be simpler to leave those axes enabled if you only ever plan on changing them once or twice because the CPU cost for checking that change isn't as expensive as the serialization and state update sending process. The associated axis threshold values can make the biggest impact on frequency of sending state updates that, in turn, will reduce the number of state updates sent per second at the cost of losing some motion resolution.

:::info
The __Axis to Synchronize__ properties that determine which axes are synchronized don't get synchronized with other instances. If you change ownership and there have been any adjustments to these values that are different from the network prefab's original settings, you'll need to keep those values synchronized and apply them upon the notification that ownership has changed.
:::

### Authority

![image](../../images/networktransform/AuthorityMode.png)

The authority mode of a NetworkTransform determines who is the authority over changes to the Transform state. This setting only applies when using a [client-server network topology](../../terms-concepts/client-server.md) because in a [distributed authority network topology](../../terms-concepts/distributed-authority.md) Netcode for GameObjects automatically sets the owner authority for every NetworkTransform. If you plan on developing for both network topologies then you can use this setting to preserve authority (whether server or owner) for the client-server network topology.

By default, NetworkTransform operates in server-authoritative mode. This means that changes to the Transform axis (marked to be synchronized) are detected on the server-side and state updates are pushed to connected clients. This also means any changes to the Transform axis values are overridden by the authoritative state (in this case the server-side Transform state).

For example, if you've marked only the position and rotation axis to be synchronized, but excluded all scale axis on a NetworkTransform component for a network prefab, then when you spawn an instance of the network prefab the initial authoritative side scale values are synchronized upon spawning. From that point forward, the non-authoritative instances (in this case the client-side instances) will maintain those same scale axis values even though they are never updated again.

#### Client vs server authority

When using a client-server network topology, you have the option of making the server or the owner (which would include clients) the authority over NetworkTransform state updates. This is referred to as the authority motion model. You can have an owner or server authority motion model per instance, but you can mix and match the right motion model based on the network prefab type being spawned.

- The most common use for an owner authority motion model is for player prefabs, when you want the player to have a more immediate response to their inputs.
- The most common use for a server authority motion model is for things like AI, NPCs, items that can be picked up, and moving world objects (such as elevators, platforms, and doors).

:::info
When mixing authority motion models and using physics, latency will impact how (and when) things collide and requires additional consideration and planning.
:::

### Thresholds

![image](../../images/networktransform/Thresholds.png)

You can use the threshold values to set a minimum threshold value for synchronizing changes. This can be help reduce the frequency of synchronization updates by only synchronizing changes above or equal to the threshold values (changes below won't be synchronized).

For example, if your NetworkTransform has [__Interpolate__](#interpolation) enabled, you might find that you can lower your position threshold resolution (by increasing the position threshold value) without impacting the "smoothness" of an object's motion while also reducing the frequency of position updates (thus reducing the bandwidth cost per instance). Increasing the threshold resolution (by lowering the position threshold value) increases the potential frequency of when the object's position will be synchronized (and will increase the bandwidth cost per instance).

> [!NOTE]
> Threshold values are not synchronized, but they can be updated on the [authoritative instance](#authority). You should keep this in mind when using owner authoritative mode instances, since changing ownership will use whatever values are currently set on the new owner instance. If you plan on changing the threshold value during runtime and plan on changing ownership, then you might need to synchronize threshold values as well.

### Delivery

![image](../../images/networktransform/Delivery.png)

#### Tick Synchronize Children

[Thresholds](#thresholds) determine when the authority of a NetworkTransform will detect a large enough change to send a state update. However, not all motion is identical and the rate that a child NetworkTransform is updated might not be the same as its parent. When this happens, it can cause a slight jitter with children of a parent NetworkTransform.

When __Tick Sync Children__ is enabled, the top-most parent NetworkTransform automatically forces any nested NetworkTransform components under it to synchronize at the same time as the top-most parent does. This ensures that all nested NetworkTransform components are synchronized with the top-most parent on the exact same network tick and that they are synchronized in order.

> [!NOTE]
> __Tick Sync Children__ does not apply to parented NetworkObjects with NetworkTransforms but does apply to any nested NetworkTransforms.

#### Network conditions to consider

Sometimes network conditions are poor, with packets experiencing latency and potentially packet loss. When NetworkTransform [interpolation](#interpolation) is enabled, packet loss can mean undesirable visual artifacts such as stutter. To try and mitigate these issues, NetworkTransform defaults to sending delta state updates (such as position, rotation, or scale changes) as unreliable sequenced network-delivered messages. This ensures that if one state is lost then the `BufferedLinearInterpolator` can recover easily, because it doesn't have to wait precisely for the next state update and can just lose a small portion of the overall interpolated path. For example, with a `TickRate` setting of 30, you could lose 5 to 10% of the overall state updates over one second and still have a relatively similar interpolated path to that of a perfectly delivered 30 delta state updates generated path. The [__UseUnreliableDeltas__](#use-unreliable-deltas) NetworkTransform property, which defaults to disabled, controls whether you send your delta state updates unreliably or reliably.

Of course, you might wonder what would happen if 5% of the end of a jumping motion were dropped and how NetworkTransform might recover since each state update sent is only based on axial deltas defined by each axis threshold setting. The answer is that there is a small bandwidth penalty for sending standard delta state updates unreliably,  full axial frame synchronization, which assures that in the event there is loss each NetworkTransform will be "auto-corrected" once per second.

> [!NOTE]
> When using a NetworkRigidbody or NetworkRigidbody2D component with the __Use Rigidbody for Motion__ property enabled, you should avoid using the __UseUnreliableDeltas____ NetworkTransform property because it can impact the overall interpolation result when you have multiple Rigidbody-based objects that need to keep relatively synchronized with each other.

#### Unreliable State Updates

When unreliable state updates are enabled, NetworkTransform instances are assigned a constantly rolling tick position relative to a one-second period of time. If you're using the default `NetworkConfig.TickRate` value (30), then there are 30 "tick slots" that each NetworkTransform instance is distributed amongst on the authoritative instance. This means that each instance sends one axial frame synchronization update per second while the NetworkObject in question is moving, rotating, or scaling enough to trigger delta state updates. When a NetworkObject comes to rest (and is no longer sending delta state updates) the axial frame synchronization stops. This ensures that if a vital portion of a state update is dropped, within a one-second period of time, all axes marked to be synchronized will be synchronized to provide an eventual consistency in axis synchronization between the authority and non-authority instances.

> [!NOTE]
> If bandwidth consumption becomes a concern and you have tested your project under normal network conditions with `UseUnreliableDeltas` disabled with no noticeable visual artifacts, then you can opt out of unreliable delta state updates to recover the minor penalty for being packet loss tolerant. Or you can opt to make that an in-game configuration setting that players can enable or disable. You just need to update the authoritative NetworkTransform instances with any change in the setting during runtime.

### Configurations

![image](../../images/networktransform/Configurations.png)

#### In Local Space

By default, NetworkTransform synchronizes the Transform of an object in world space. The __In Local Space__ configuration option allows you to change to synchronizing the transform in local space instead. A child's local space axis values (position and rotation primarily) are always relative offsets from the parent transform, where a child's world space axis values include the parent's axis values. Where a transform with no parent really is "parented" to the root of the scene hierarchy which results in its world and local space positions to always be the same.

Enabling the __In Local Space__ property on a parented NetworkTransform can improve the synchronization of the transform when the object gets re-parented because the re-parenting won't change the local space Transform of the object (but does change the world space position) and you only need to update motion of the parented NetworkTransform relative to its parent (if the parent is moving and the child has no motion then there are no delta states to detect for the child but the child moves with the parent).

:::info
The authority instance does synchronize changes to the __In Local Space__ property. As such, you can make adjustments to this property on the authoritative side during runtime and the non-authoritative instances will automatically be updated.
:::

#### Switch Transform Space When Parented

When changing from world space to local space and vice versa, NetworkTransform changes which values of a Transform to use when detecting changes to each synchronized axis. While it seems simple enough to just change the __In Local Space__ property when a NetworkObject is parented, it can become tricky trying to make sure the transition is smooth for all clients. This is especially the case when you have the [__Interpolate__](#interpolation) property enabled, because non-authority instances are using the `BufferedLinearInterpolator` to interpolate from one state update to the next and doing this while several network ticks behind the authority instance.

This means that non-authority instances could still have state updates pending to be processed when a NetworkObject is parented (or de-parented) and those buffered state values are still expressed as world (or local) space values. Since parenting is not network tick synchronized, the non-authority instances could still have the previous (world or local space) state updates remaining to be processed. This can create a visual "popping" result on the non-authority instance because it has been placed in a different Transform space while processing the previous Transform space state updates.

To resolve this issue, you can enable the __Switch Transform Space When Parented__ configuration property and the NetworkTransform will automatically detect when its NetworkObject has changed parented status and convert the pending states within each respective axis's `BufferedLinearInterpolator` to the appropriate Transform space values. The end result yields a seamless transition between world and local (and vice versa) when parenting.

### Interpolation

![image](../../images/networktransform/Interpolation.png)

Interpolation is enabled by default and is recommended if you desire smooth transitions between Transform updates on non-authoritative instances. Interpolation buffers incoming state updates that can introduce a slight delay between the authority and non-authority instances. When the __Interpolate__ property is disabled, changes to the transform are immediately applied on non-authoritative instances, which can result in visual jitter and/or objects jumping to newly applied state updates when latency is high. Changes to the __Interpolation__ property during runtime on the authoritative instance will be synchronized with all non-authoritative instances. Of course, you can increase the network tick to a higher value in order to get more samples per second, but that still will not yield an over-all smooth motion on the non-authoritative instances and will only consume more bandwidth.

There are three types of interpolation types to chose from when the __Interpolate__ property is enabled:

- [__Legacy lerp__](#legacy-lerp-interpolator-type): The original (now legacy) lerping approach. Selected by default.
- [__Lerp__](#lerp-and-smooth-dampening-interpolator-types): An improved lerping approach that ensures the interpolator gets much closer to its destination (compared to the legacy lerp).
- [__Smooth dampening__](#lerp-and-smooth-dampening-interpolator-types): An alternative approach to interpolation that smooth dampens towards the target value while taking the value's rate of change into consideration.

#### Lerp smoothing

All interpolation types provide you with the ability to enable or disable lerp smoothing. Lerp smoothing provides you with a finer smoothing pass at the end of an interpolator's update, but this can be at the expense of precision depending on the value of the relative interpolator's max interpolation time.

##### Slerp position

![image](../../images/networktransform/PositionSlerp.png)

When this property and __Interpolation__ are both set, non-authoritative instances will [slerp](https://docs.unity3d.com/ScriptReference/Vector3.Slerp.html) towards their destination position rather than [lerping](https://docs.unity3d.com/ScriptReference/Vector3.Lerp.html). Slerping is typically used when your object is following a circular and/or spline-based motion path and you want to preserve the curvature of that path. Since lerping between two points yields a linear progression over a line between two points, there can be scenarios where the frequency of delta position state updates could yield a loss in the curvature of an object's motion.

> [!NOTE]
> The NetworkTransform component only interpolates client-side. For smoother movement on the host or server, you can implement interpolation server-side as well. While the server won't have the jitter caused by the network, some stutter can still happen locally (for example, movement done in `FixedUpdate` with a low physics update rate).

#### Legacy lerp interpolator type

This is the original linear interpolation approach which guarantees that any existing project using this interpolator will yield the exact same results. This is the default interpolator selected.

This interpolator can lose some of the precision when interpolating between two state updates. This is typically most noticeable with position when there's a rapid change in direction (such as a bouncing ball colliding against a floor). When this happens you can end up with the non-authority instances never reaching their final destination before heading towards their new destination (making it appear that the ball bounces a bit above the floor for non-authority instances).

Legacy lerp handles lerp smoothing differently than the other two interpolator types. The associated maximum interpolation time is inversely proportional to the amount of lerp smoothing that will be applied and is impacted by frame time. This can be represented by the following formula:

`t = delta time / max interpolation time`
_Where t is the lerp "t" parameter._

If you have a max interpolation time of 0.5 and a delta time of 0.0167777 (equivalent to 60fps), then your "t" value would be 0.032, which, depending on how large your state delta value is (what you are lerping towards), would take much longer than the typical tick rate (30 or 0.033333ms) to reach the current state's value. As such, the interpolator would have moved on to the next state update (if one is pending and ready to be processed) well before the current state update's value was reached. There are scenarios where you might want this type of behavior where you are adjusting the max interpolation time dynamically during runtime (for example, the slower something is moving the smoother you might want it to be).

However, if you are setting it within the Editor as a default value that will never be updated again, then keeping the range between 0.2 and 0.02 is recommended (where 0.01 will almost always yield a value higher than 1, which using the clamped lerp will be the same as 1).

#### Lerp and smooth dampening interpolator types

When compared to the [legacy lerp interpolator type](#legacy-lerp-interpolator-type), the new lerp and smooth dampening interpolator types have the following properties:

- They both use a different approach when processing state updates.
- They both interpolate closer to the final target values and handle buffer consumption differently.
    - Both use a tick latency value to adjust how far back in the pending states to begin processing. This is automatically adjusted based on the client's latency with respect to the server or distributed authority service.
- They can both be used with _FixedUpdate_.
    - Their processing time is adjusted based on the _FixedUpdate_ time consumed. If _FixedUpdate_ is invoked multiple times in a single frame, they will both interpolate at that rate.
- The maximum interpolation time property for both interpolator types is not impacted by frame time.

When using lerp and smooth dampening interpolator types, the maximum interpolation time for lerp smoothing can be calculated as such:

`t = 1.0f - (max interpolation time)`
_Where the value of "t" is clamped between 0.0f and 1.0f._

Both lerp and smooth dampening use their axis-relative lerp methods (Vector3 or Quaternion).

__Lerp__
The lerp interpolation type, like that of the legacy lerp, uses [Vector3.Lerp](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Vector3.Lerp.html) and [Quaternion.Lerp](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Quaternion.Lerp.html) interpolation approaches.

__Smooth Dampening__
This interpolation type uses [Vector3.SmoothDamp](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Vector3.SmoothDamp.html) for position and scale, and uses [Mathf.SmoothDampAngle](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mathf.SmoothDampAngle.html) based on a quaternion's Euler angles.

__Both interpolator types will:__
- Interpolate towards the current state's target (using their respective methods provided above).
- If enabled, apply lerp smoothing so that the current axis relative value is lerped towards the current interpolated value at a rate of 1.0f - (max interpolation time) each update.

#### Choosing and configuring an interpolator type

If you haven't already balanced gameplay based on the (original) [legacy lerp type](#legacy-lerp-interpolator-type), then it's recommended that use the lerp interpolator type.

__Runtime maximum interpolation time__
You can adjust the maximum interpolation time for any axis of a NetworkTransform during runtime by adjusting any of the following properties on non-authoritative instances:

- `NetworkTransform.PositionMaxInterpolationTime`
- `NetworkTransform.RotationMaxInterpolationTime`
- `NetworkTransform.ScaleMaxInterpolationTime`

This can be useful for edge case scenarios where things like object collision, jumping, and other similar scenarios could benefit from having a moment where accuracy is preferred over smoothing or you want to provide your players with the ability to make adjustments to suit their preferences.

__Lerp and smooth dampening__
If you're not yet sure what kind of response you need from your interpolator, then setting the maximum interpolation time to 0.5 will provide you with a reasonably smooth end result while still reasonably preserving the precision of each state update. Lerp will start to have more jitter on non-authority instances the closer its maximum interpolation time reaches 0.0f (the same as no smoothing). Smooth dampening can help preserve the smooth end result on non-authoritative instances as its maximum interpolation time approaches 0.0f.

__Physics__
If you want to have more precise collisions between kinematic and non-kinematic bodies (which can often be an issue when using NetworkRigidbody) and you're using a client-server network topology with owner authoritative NetworkTransform instances or you're using a distributed authority network topology, then you need to consider what kind of interpolator type you're using, the maximum interpolation time value (if smooth lerp is enabled), and what your Rigidbody component's settings are.

For the best results, at the expense of additional processing time, setting the Rigidbody __Interpolate__ property to _Extrapolate_ and the __Collision Detection__ property to _Continuous Dynamic_ can help improve your overall end results when it comes to collision detection. However, you should always consider your maximum interpolation time value as being the first area to adjust before seeking more advanced options.

### Use Quaternion Synchronization

By default, rotation deltas are synchronized using Euler values and this is sufficient for many situations. However, there are scenarios where synchronizing Euler deltas yields undesirable results. If you have complex nested NetworkTransforms where there are varying rotations between the parents and children transforms, then you add [interpolation](#interpolation) into the mix (interpolation being buffered and having an inherent delay between the non-authoritative intance's current rotation and the target rotation), there are adjustments that occur immediately within a quaternion that handle more complex transform related issues (such as Gimbal Lock).

With quaternion synchronization enabled, the authoritative instance still compares threshold values against the Euler axis values to determine if an update in a Transform's rotation is needed, but the entire quaternion itself is updated instead of just the Euler axis where the change(s) is/are detected. This guarantees that the proper rotation for an object will be applied to non-authoritative instances and the changes will have already accounted for more complex issues that can arise with Euler angles.

Quaternion synchronization comes with a price, however. It increases the bandwidth cost, 16 bytes per instance, in exchange for handling the more complex rotation issues that often occur when using nested NetworkTransform (one or more parent transforms with one or more child transforms). However, when you enable the __Use Quaternion Synchronization__ property you will notice a change in both the __Syncing__ axis selection check boxes and a new __Use Quaternion Compression__ property will appear:

![image](../../images/networktransform/NetworkTransformQuaternionSynch.png)

:::note
The rotation synchronization axis checkboxes are no longer available when __Use Quaternion Synchronization__ is enabled (since synchronizing the quaternion of a transform always updates all rotation axes) and __Use Quaternion Compression__ becomes a visible option.
:::

### Use Quaternion Compression

Because synchronizing a quaternion can increase the bandwidth cost per update of a NetworkTransform's rotation state, there are two ways to reduce the overall bandwidth cost of quaternion synchronization:

- __Quaternion Compression__: This provides the highest compression (16 bytes reduced down to 4 bytes per update) with a slightly higher precision loss than half float precision.
- __Half Float Precision__: When enabled and __Use Quaternion Compression__ is disabled, this provides an alternate mid-level compression (16 bytes reduced down to 8 bytes per update) with less precision than full float values but higher precision than quaternion compression.

Quaternion compression is based on a smallest three algorithm that can be used when rotation precision is less of a concern than bandwidth cost. You might have ancillary objects/projectiles that require some form of rotation synchronization, but in the overall scheme of your project do not require perfect alignment. If bandwidth cost and precision are both a concern, then the alternate recommended compression is half float precision. It's also recommended that you try out the different compression options: you might find that the fractional loss in precision is perfectly acceptable for your project's needs (with the bonus of being able to reduce the overall bandwidth cost by up to 50% for all instances than when using full precision).

This property value can be updated on the authority during runtime and will be synchronized on all non-authority instances. Remember, however, that updating this value during runtime on the authoritative instance will result in a full synchronization of the NetworkTransform and all non-authority instances will have their interpolators reset.

### Use half float precision

Enabling this property converts any transform axial value from a 4 byte float to a 2 byte half-float at the expense of precision. When this option is enabled, half float precision is used for all transform axes marked for synchronization. However, there are some unique aspects of half float precision when it comes to position and rotation.

Since there is a loss in precision, position state updates only provide the delta in position relative to the last known full position. The `NetworkDeltaPosition` serializable structure keeps track of the current delta between the last known full position and the current delta offset from the last known full position. Additionally, `NetworkDeltaPosition` auto-corrects precision loss by determining any loss of precision on the authoritative side when sending an update. Any precision delta from the previous update will be included in the next position update.

In other words, non-authoritative instances can potentially have a fractional delta (per applied update) from the authoritative instance for the duration of 1 network tick period or until the next transform state update is received. Additionally, `NetworkDeltaPosition` bridges the gap between the [maximum half float value](https://github.com/Unity-Technologies/Unity.Mathematics/blob/701d58fde76f3b93e40d0a792cd8fa4c130f1450/src/Unity.Mathematics/half.cs#L25) and the maximum boundaries of the Unity World space (global/project scale relative).

> [!NOTE]
> __Recommended Unity World Space Units Per Second:__
The maximum delta per update should not exceed 64 Unity world space units. If you're using the default network tick (30) then an object should not move at speeds that are equal to or exceed 1,920 Unity world space units per second (30 x 64). For reference, the default camera far clipping plane is 1,000 Unity world space units which means something moving at 1,920 Unity world space units would most likely not be visually detectable or appear as a brief "blip" in the render view frustum.

When __Use Quaternion Synchronization__ and __Use Half Float Precision__ are both enabled and __Use Quaternion Compression__ is disabled, the quaternion values are synchronized via the `HalfVector4` serializable structure where each axial value (x, y, z, and w) are stored as [half values](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.half.html). This means that each rotation update is reduced from a full precision 16 bytes per update down to 8 bytes per update. Using half float precision for rotation provides a better precision than quaternion compression at 2x the bandwidth cost but half the cost of full precision.

When __Use Quaternion Synchronization__, __Use Half Float Precision__, and __Use Quaternion Compression__ are enabled, quaternion compression is used in place of half float precision for rotation.

All of these properties are synchronized to non-authoritative instances when updated on the authoritative instance.

## Additional virtual methods of interest

- `NetworkTransform.OnAuthorityPushTransformState`: This virtual method is invoked when the authoritative instance is pushing a new `NetworkTransformState` to non-authoritative instances. This can be used to better determine the precise values updated to non-authoritative instances for prediction-related tasks.

- `NetworkTransform.OnNetworkTransformStateUpdated`: This virtual method is invoked when the non-authoritative instance is receiving a pushed `NetworkTransformState` update from the authoritative instance. This can be used to better determine the precise values updated to non-authoritative instances for prediction-related tasks.

- `NetworkTransform.Awake`: This method has been made virtual to provide the ability to do any custom initialization. If you override this method, you are required to invoke `base.Awake()` (recommended invoking it first).

- `NetworkTransform.OnInitialize`: This virtual method is invoked when the associated NetworkObject is first spawned and when ownership changes.

- `NetworkTransform.Update`: This method has been made virtual to provide the ability to handle any customizations to a derived NetworkTransform class. If you override this method, it's required that all non-authoritative instances invoke `base.Update()` but not required for authoritative instances.
