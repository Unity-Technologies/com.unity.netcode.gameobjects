# NetworkTransform

The synchronization of an object's transform is one of the core netcode tasks. The outline of operations is simple:
- Determine which transform axis you want to have synchronized
- Serialize the values
- Send the serialized values as messages to all other connected clients
- Process the messages and deserialize the values
- Apply the values to the appropriate axis

Except the implementation is far more complicated than one might expect. For example, we need to take into consideration:
- Who controls the synchronization (i.e. each client or the server or perhaps both depending upon the object being synchronized)?
- How often do you synchronize the values?
- What logic should you use to determine when and which values need to be synchronized?
- For objects with one or more child objects, should you synchronize world or local space axis values?
- How can you optimize the bandwidth for a transform update?

Fortunately, Netcode for GameObjects provides you with the NetworkTransform component. It handles many of the tricky aspects of transform synchronization and can be configured via Inspector properties.

## Adding the component

When adding a NetworkTransform component to a GameObject, it requires a NetworkObject on the same or a parent GameObject for it to function.

In this image both NetworkTransform and NetworkObject components are on the same GameObject:

![image](../images/NetworkTransformSimple.png)

Alternatively, a parent GameObject can have the NetworkObject component while the NetworkTransform is attached to a child object:

![image](../images/NetworkTransformSimpleParent.png)

You can also have NetworkTransform components on several child objects, all sharing the same NetworkObject in their common parent object:

![image](../images/NetworkTransformNestedParent.png)

With such nested NetworkTransforms you can theoretically have a NetworkTransform on every child object. _However, we recommend exercising caution with the amount of nested NetworkTransforms in a network prefab. Particularly if there will be many instances of this network prefab._

**The general rule to follow is:**

 As long as there is at least one (1) NetworkObject at the same GameObject hierarchical level or above, you can attach a NetworkTransform component to a GameObject.

 _You could have a single root-parent GameObject that has a NetworkObject component and under the root-parent several levels of nested child GameObjects that all have NetworkTransform components attached to them. Each child GameObject would not require a NetworkObject component in order for each respective NetworkTransform component to function/synchronize properly._

## Configuring

When you select a NetworkTransform component, you will see the following properties in the inspector view:

![image](../images/NetworkTransformProperties.png)

### Property synchronization

Some of the `NetworkTransform` properties are automatically synchronized by the authoritative instance to all non-authoritative instances.  It is **important to note** that when any synchronized property changes the NetworkTransform is effectively "teleported" (i.e. all values re-synchronized and interpolation is reset) which can cause a single frame delta in position, rotation, and scale (depending upon what is being synchronized). _Always keep this in mind when making adjustments to NetworkTransform properties during runtime._

### Synchronizing

You often don't need to synchronize all transform values of a GameObject over the network. For instance, if the scale of the GameObject never changes, you can deactivate it in the **syncing scale** row in the Inspector. Deactivating synchronization saves CPU costs and network bandwidth.

The term "synchronizing" refers to the synchronization of axis values over time. This is not to be confused with the initial synchronization of a transform's values. As an example:
 If you don't plan on changing the transform's scale after the initial first synchronization (i.e. upon joining a network session or when a network prefab instance is spawned for the first time), then un-checking/disabling the X, Y, and Z properties for Scale synchronization would remove the additional processing overhead per instance.

Since these values really only apply to the authoritative instance, changes can be made to these values during runtime and non-authoritative instances will only receive updates for the axis marked for synchronization on the authoritative side.

### Thresholds

You can use the threshold values to set a minimum threshold value. This can be used to help reduce the frequency of synchronization updates by only synchronizing changes above or equal to the threshold values (changes below won't be synchronized). As an example:

If your `NetworkTransform` has `Interpolate` enabled, you might find that you can lower your position threshold resolution (i.e. by increasing the position threshold value) without impacting the "smoothness" of an object's motion while also reducing the frequency of position updates (i.e. reduces the bandwidth cost per instance). Increasing the threshold resolution (i.e. by lowering the position threshold value) will increase the potential frequency of when the object's position will be synchronize (i.e. can increase the bandwidth cost per instance).

> [!NOTE]
> Threshold values are not synchronized, but they can be updated on the [authoritative instance](networktransform#authority-modes). You should keep this in mind when using [owner authoritative mode](networktransform#owner-authoritative-mode) instances since changing ownership will use whatever values are currently set on the new owner instance. If you plan on changing the threshold value during runtime and plan on changing ownership, then you might need to synchronize the threshold values as well.

### Delivery

Sometimes network conditions are not exactly "optimal" where packets can have both undesirable latency and even the dreaded packet loss. When NetworkTransform interpolation is enabled, packet loss can mean undesirable visual artifacts (_i.e. large visual motion gaps often referred to as "stutter"_). Originally, NetworkTransform sent every state update using reliable fragmented sequenced network delivery. For interpolation, with enough latency and packet loss this could cause a time gap between interpolation points which eventually would lead to the motion "stutter". Fortunately, NetworkTransform has been continually evolving and defaults to sending the more common delta state updates (_i.e. position, rotation, or scale changes_) as unreliable sequenced network delivered messages. If one state is dropped then the `BufferedLinearInterpolator` can recover easily as it doesn't have to wait precisely for the next state update and can just lose a small portion of the over-all interpolated path (_i.e. with a TickRate setting of 30 you could lose 5 to 10% of the over-all state updates over one second and still have a relatively similar interpolated path to that of a perfectly delivered 30 delta state updates generated path_). As such, the UseUnreliableDeltas NetworkTransform property, default to enabled, controls whether you send your delta state updates unreliably or reliably.

Of course, you might wonder what would happen if say 5% of a jumping motion, towards the end of the jumping motion, were dropped how NetworkTransform might recover since each state update sent is only based on axial deltas defined by each axis threshold settings. The answer is that there is a small bandwidth penalty for sending standard delta state updates unreliably: Axial Frame Synchronization.

#### Axial frame synchronization

When unreliable delta state updates is enabled (UseUnreliableDeltas is enabled), NetworkTransform instances are assigned a constantly rolling tick position relative to a 1 second period of time. So, if you are using the default `NetworkConfig.TickRate` value (30) there are 30 "tick slots" that each NetworkTransform instance is distributed amongst on the authoritative instance. This means that each instance will send 1 Axial Frame Synchronization update per second while the NetworkObject in question is moving, rotating, or scaling enough to trigger delta state updates. When a NetworkObject comes to rest (i.e. no longer sending delta state updates) the Axial Frame Synchronization stops. This assures that if a vital portion of a state update is dropped, within a 1 second period of time, all axis marked to be synchronized will be synchronized to provide an eventual consistency in axis synchronization between the authority and non-authority instances.

> [!NOTE]
> If bandwidth consumption becomes a concern and you have tested your project under normal network conditions with UseUnreliableDeltas disabled with no noticeable visual artifacts, then you can opt out of unreliable delta state updates to recover the minor penalty for being packet loss tolerant or you might opt to make that an in-game configuration setting that players can enable or disable. You just need to update the authoritative NetworkTransform instances with any change in the setting during runtime.

### Local space

By default, `NetworkTransform` synchronizes the transform of an object in world space. The **In Local Space** configuration option allows you to change to synchronizing the transform in local space instead. A child's local space axis values (position and rotation primarily) are always relative offsets from the parent transform. Where a child's world space axis values include the parent's axis values.

Using **local space** on a parented NetworkTransform can improve the synchronization of the transform when the object gets re-parented because the re-parenting won't change the **local space** transform of the object but does change the **world space** position.

> [!NOTE]
> The authority instance does synchronize changes to the LocalSpace property. As such,you can make adjustments to this property on the authoritative side during runtime and the non-authoritative instances will automatically be updated.

### Interpolation

Interpolation is enabled by default and is recommended if you desire smooth transitions between transform updates on non-authoritative instances.Interpolation will buffer incoming state updates that can introduce a slight delay between the authority and non-authority instances. When the **Interpolate** property is disabled, changes to the transform are immediately applied on non-authoritative instances which can result in a visual "jitter" and/or seemingly "jumping" to newly applied state updates when latency is high.

Changes to the **Interpolation** property during runtime on the authoritative instance will be synchronized with all non-authoritative instances.

> [!NOTE]
> The `NetworkTransform` component only interpolates client-side. For smoother movement on the host or server, users might want to implement interpolation server-side as well. While the server won't have the jitter caused by the network, some stutter can still happen locally (for example, movement done in `FixedUpdate` with a low physics update rate).

![Graphic of a buffered tick between the server and a client (that is, interpolation)](../images/BufferedTick.png)

### Slerp position

When this property and **Interpolation** are both set, non-authoritative instances will [Slerp](https://docs.unity3d.com/ScriptReference/Vector3.Slerp.html) towards their destination position as opposed to [Lerp](https://docs.unity3d.com/ScriptReference/Vector3.Lerp.html). Typically this can be used when your object is following a circular and/or spline based motion path and you want to preserve the curvature of that path. Since "lerping" between two points yields a linear progression over a line between two points, there can be scenarios where the frequency of delta position state updates could yield a loss in the curvature of an object's motion.  

### Use quaternion synchronization

By default, rotation deltas are synchronized using Euler values. For many scenarios, using Euler values might be all that is needed. However, there are scenarios where synchronizing Euler deltas will yield undesirable results. One scenario is when you have complex nested NetworkTransforms where there are varying rotations between the parents and children transforms. When you add interpolation into the mix (_remember interpolation is buffered and has an inherent delay between the non-authoritative's current rotation and the target rotation_), there are adjustments that occur immediately within a Quaternion that handle more complex transform related issues (i.e. Gimbal Lock, etc).

With Quaternion synchronization enabled, the authoritative instance still compares threshold values against the Euler axis values to determine if an update in a transform's rotation is needed but the entire Quaternion itself is updated as opposed to just the Euler axis where the change(s) is/are detected. This means that you are guaranteed the proper rotation for an object will be applied to non-authoritative instances and the changes will have already accounted for more complex issues that can arise with Euler angles.

Quaternion synchronization comes with a price. It will increase the bandwidth cost, 16 bytes per instance, in exchange for handling the more complex rotation issues that more often occur when using nested NetworkTransform (one or more parent transforms with one or more child transforms). However, when you enable the **Use Quaternion Synchronization** property you will notice a change in both the **Syncing** axis selection check boxes and a new **Use Quaternion Compression** property will appear:

![image](../images/NetworkTransformQuaternionSynch.png)

> [!NOTE]
> The rotation synchronization axis checkboxes are no longer available when **Use Quaternion Synchronization** is enabled (_since synchronizing the quaternion of a transform will always update all rotation axis_) and **Use Quaternion Compression** becomes a visible option.

### Use quaternion compression

Since synchronizing a quaternion can increase the bandwidth cost per update of a `NetworkTransform`'s rotation state, there are two ways to reduce the over-all bandwidth cost of quaternion synchronization:
- **Quaternion Compression:** This provides the highest compression (16 bytes reduced down to 4 bytes per update) with a slightly higher precision loss than half float precision.
- **Half Float Precision:** When enabled and **Use Quaternion Compression** is disabled, this provides an alternate mid-level compression (16 bytes reduced down to 8 bytes per update) with less precision than full float values but higher precision than quaternion compression.

Quaternion compression is based on a smallest three algorithm that can be used when rotation precision is less of a concern than the bandwidth cost. You might have ancillary objects/projectiles that require some form of rotation synchronization, but in the over-all scheme of your project do not require perfect alignment. If bandwidth cost and precision are both a concern, then the alternate recommended compression is half float precision. It is also recommended to try out the different compression options, you might find that the fractional loss in precision is perfectly acceptable for your project's needs (_with the bonus of being able to reduce the over-all bandwidth cost by up to 50% for all instances than when using full precision_).

This property value can be updated on the authority during runtime and will be synchronized on all non-authority instances. _Reminder: Updating this value during runtime on the authoritative instance will result in a full synchronization of the NetworkTransform and all non-authority instances will have their interpolators reset._

### Use half float precision

Enabling this property does exactly what it sounds like, it converts any transform axial value from a 4 byte float to a 2 byte half-float at the expense of a loss in precision. When this option is enabled, half float precision is used for all transform axis marked for synchronization. However, there are some unique aspects of half float precision when it comes to position and rotation.

Since there is a loss in precision, position state updates only provide the delta in position relative to the last known full position. The `NetworkDeltaPosition` serializable structure keeps track of the current delta between the last known full position and the current delta offset from the last known full position. Additionally, `NetworkDeltaPosition` auto-corrects precision loss by determining any loss of precision on the authoritative side when sending an update. Any precision delta from the previous update will be included in the next position update. _In other words, non-authoritative instances can potentially have a fractional delta (per applied update) from the authoritative instance for the duration of 1 network tick period or until the next transform state update is received._ Additionally, `NetworkDeltaPosition` bridges the gap between the [maximum half float value](https://github.com/Unity-Technologies/Unity.Mathematics/blob/701d58fde76f3b93e40d0a792cd8fa4c130f1450/src/Unity.Mathematics/half.cs#L25) and the maximum boundaries of the Unity World space (global/project scale relative).

> [!NOTE]
> **Recommended Unity World Space Units Per Second:**
> The maximum delta per update should not exceed 64 Unity world space units. If you are using the default network tick (30) then an object should not move at speeds that are equal to or exceed 1,920 Unity world space units per second (i.e. 30 x 64). To give you a frame of reference, the default camera far clipping plane is 1,000 Unity world space units which means something moving at 1,920 Unity world space units would most likely not be visually detectable or appear as a brief "blip" in the render view frustum.

When **Use Quaternion Synchronization** and **Use Half Float Precision** are both enabled and **Use Quaternion Compression** is disabled, the quaternion values are synchronized via the `HalfVector4` serializable structure where each axial value (x, y, z, and w) are stored as [half values](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.half.html). This means that each rotation update is reduced from a full precision 16 bytes per update down to 8 bytes per update. Using half float precision for rotation provides a better precision than quaternion compression at 2x the bandwidth cost but half the cost of full precision.

When **Use Quaternion Synchronization**, **Use Half Float Precision**, and **Use Quaternion Compression** are enabled, quaternion compression is used in place of half float precision for rotation.

All of these properties are synchronized to non-authoritative instances when updated on the authoritative instance.


## Authority modes

### Server authoritative mode

By default, `NetworkTransform` operates in server authoritative mode. This means that changes to transform axis (marked to be synchronized) are detected on the server-side and pushed to connected clients. This also means any changes to the transform axis values will be overridden by the authoritative state (in this case the server-side transform state).

There is another concept to keep in mind about axis synchronization vs the initial synchronized transform values. Any axis not marked to be synchronized will still be updated with the authority's initial state when a NetworkObject is spawned or when a client is synchronized for the first time.

> [!NOTE]
> For example, if you have marked only the position and rotation axis to be synchronized but exclude all scale axis on a NetworkTransform component for a network prefab. When you spawn an instance of the network prefab the initial authoritative side scale values are synchronized upon spawning. From that point forward, the non-authoritative instances (in this case the client-side instances) will maintain those same scale axis values even though they are never updated again.

### Owner authoritative mode
**(a.k.a. ClientNetworkTransform)**

 Server-side authority NetworkTransforms provide a balance between synchronized transforms and the latency between applying the updates on all connected clients. However, there are times when you want the position to update immediately for a specific NetworkObject (common the player) on the client-side. Owner authority of a NetworkTransform is dictated by the `NetworkTransform.OnIsServerAuthoritative` method when a NetworkTransform component is first initialized. If it returns `true` (the default) then it initializes as a server authoritative `NetworkTransform`. If it returns `false` then it initializes as an owner authoritative `NetworkTransform` (a.k.a. `ClientNetworkTransform`). This can be achieved by deriving from `NetworkTransform`, overriding the `OnIsServerAuthoritative` virtual method, and returning false like in the code example below:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Packages/com.unity.multiplayer.samples.coop/Utilities/Net/ClientAuthority/ClientNetworkTransform.cs
```

Netcode for GameObjects also comes with a sample containing a `ClientNetworkTransform`. This transform synchronizes the position of the owner client to the server and all other client allowing for client authoritative gameplay.

You can use the existing `ClientNetworkTransform` in the Multiplayer Samples Utilities package.<br />

To add the Multiplayer Samples Utilities package:

* Open the Package Manager by selecting **Window** > **Package Manager**.
* Select **Add** (+) > **Add from git URL…**.
* Copy and paste the following Git URL: `https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git?path=/Packages/com.unity.multiplayer.samples.coop#main`
* Select **Add**.

Optionally, you can directly add this line to your `manifest.json` file:

`"com.unity.multiplayer.samples.coop": "https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git?path=/Packages/com.unity.multiplayer.samples.coop#main"`

## Additional virtual methods of interest

`NetworkTransform.OnAuthorityPushTransformState`: This virtual method is invoked when the authoritative instance is pushing a new `NetworkTransformState` to non-authoritative instances. This can be used to better determine the precise values updated to non-authoritative instances for prediction related tasks.

`NetworkTransform.OnNetworkTransformStateUpdated`: This virtual method is invoked when the non-authoritative instance is receiving a pushed `NetworkTransformState` update from the authoritative instance. This can be used to better determine the precise values updated to non-authoritative instances for prediction related tasks.

`NetworkTransform.Awake`: This method has been made virtual in order to provide you with the ability to do any custom initialization. If you override this method, you are required to invoke `base.Awake()` (recommended invoking it first).

`NetworkTransform.OnInitialize`: This virtual method is invoked when the associated `NetworkObject` is first spawned and when ownership changes.

`NetworkTransform.Update`: This method has been made virtual in order to provide you with the ability to handle any customizations to a derived `NetworkTransform` class. If you override this method, it is required that all non-authoritative instances invoke `base.Update()` but not required for authoritative instances.
