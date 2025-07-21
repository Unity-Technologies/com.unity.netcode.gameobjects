# ClientDriven sample

## Client-driven movements

Making movements feel responsive while staying consistent over multiple game executables, each with their own timelines, is a challenge for many networked games.

## TLDR:

- Use [ClientNetworkTransform](../../components/networktransform.md) to sync client authoritative transform updates to the server and other clients.
- Make sure ownership is set properly on that NetworkObject to be able to move it.
- Since your clients live on different timelines (one per player), you have to be careful about who takes decisions and keep some of those decisions centralized on the server.
- DON'T FORGET to test with latency, as your game will behave differently depending on whether client or server make decisions.

## Sample

ClientDriven's aim is to create a quick sample to show responsive character movements that don't feel sluggish, even under bad network conditions.
It uses the ClientNetworkTransform component and moves your own player's position client side, [client authoritatively](../../learn/dealing-with-latency.md#allow-low-impact-client-authority). Movement is leveraged through the use of Unity's Starter Assets, the [Third Person Character Controller](https://assetstore.unity.com/packages/essentials/starter-assets-third-person-character-controller-196526).

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/ClientDriven/Assets/StarterAssets/ThirdPersonController/Scripts/ThirdPersonController.cs#L155-L162
```

### Client-side object detection for pickup with server side pickup validation

Ingredients in ClientDriven are owned by the server, since they're [shared objects](../../learn/dealing-with-latency.md#issue-world-consistency). This means if a player tries to grab an object while that object is moving, a server side range detection would sometimes fail, even though it should have succeeded (since ingredients are replicated with some lag, so the player would try to grab ingredients that are a few milliseconds behind).
To make sure this doesn't happen, the object detection done to grab an ingredient is also done client side.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/ClientDriven/Assets/Scripts/ClientPlayerMove.cs#L64-L94
```

But the pickup itself is done server side.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/ClientDriven/Assets/Scripts/ServerPlayerMove.cs#L46-L82
```

Notice how we're checking whether that object can be picked up or not (since another player can have picked it up at the same time, creating a conflict).

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/ClientDriven/Assets/Scripts/ServerPlayerMove.cs#L50-L58
```
If the object is already picked up, we're not doing anything. Your client side animations should take this into account and cancel any animations "carrying" something.

### Server-side player spawn points

In this sample, our spawn points list is server side (to have a single source of truth).
ClientNetworkTransforms can be updated by owners only, which means the server can't update the player's position directly.
This means OnNetworkSpawn, the server will need to assign a position to the player using a ClientRPC.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/ClientDriven/Assets/Scripts/ServerPlayerMove.cs#L24-L44
```

## Server physics with client-driven movements (interactions on different timelines)

All the ingredients can be bumped against by players. However, since ingredients are server driven, bumping against them with client driven movements can cause desync issues.
This sample uses NetworkRigidbody for ingredients, which sets Kinematic = true, making them immovable client side. This way, only server side movements (from the replicated player movements) will move the ingredients. Two players bumping the same ingredient will still only have one single result, calculated server side, with no risk of desync.

## Reparenting

This sample uses Netcode's automatic reparenting synchronization. By setting a NetworkObject's parent to another NetworkObject, that reparenting will be synced to connected clients.
Note that we're also setting InLocalSpace while reparenting, to make sure the client side ingredient will be tracked the same way as the server side one (else the ingredient would be carried with latency, making it appear "dragging along behind you").
An ownership change can also have been used here, but the client would have needed to ask the server for that change first.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/ClientDriven/Assets/Scripts/ServerPlayerMove.cs#L46-L61
```
