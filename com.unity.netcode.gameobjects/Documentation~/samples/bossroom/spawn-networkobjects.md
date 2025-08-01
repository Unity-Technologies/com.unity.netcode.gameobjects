# Spawn NetworkObjects in Boss Room

:::note
Required reading: [Object Spawning](../../basics/object-spawning.md).
:::

This document serves as a walkthrough of Boss Room's approach to solving the following issue: Late-joining clients entering networked sessions encountering zombie `NetworkObjects`. Zombie `NetworkObjects` represent `NetworkObjects` that are instantiated for a client due to scene loads but aren't despawned or destroyed by Netcode.

This is a particular Netcode limitation of NetworkObject spawning: `NetworkObjects` that belong to a scene object should not be destroyed until its scene has been unloaded through Netcode's scene management.

The scenario in question:

* A host loads a scene and destroys a NetworkObject that belonged to that scene.
* A client joins the host's session and loads the additive scene. This operation loads **all** the `GameObjects` included in the additive scene. The NetworkObject that was destroyed on the server won't be destroyed on the client's machine.

This scenario manifested inside Boss Room, whereby late-joining clients joining a game session encountered zombie NetworkObjects that were not destroyed over the network.

Additive scenes now contain Prefab instances of a custom spawner, [`NetworkObjectSpawner`](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetworkObjectSpawner.cs) to accommodate this visual inconsistency.

Compositionally, these additive scenes now only contain the following:

* Prefab instances of level geometry.
* Prefab instances of NetworkObjects that will **not** be despawned nor destroyed for the scene's lifetime. Examples include [BreakablePot](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Prefabs/Game/BreakablePot.prefab) and [BreakableCrystal](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Prefabs/Game/BreakableCrystal.prefab).
* Prefab instances of `NetworkObjectSpawner`, which spawn NetworkObjects that **may** be destroyed or despawned during the scene's lifetime. Examples include [Imp](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/GameData/Character/Imp/Imp.asset), [VandalImp](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/GameData/Character/VandalImp/VandalImp.asset), and [ImpBoss](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/GameData/Character/ImpBoss/ImpBoss.asset).
