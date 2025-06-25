# Real world In-scene NetworkObject parenting of players solution

We received the following issue in Github.

## Issue:

When a player Prefab has a script that dynamically adds a parent to its transform, the client can't join a game hosted by another client. [You can see original issue here](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/1211)

Steps to reproduce the behavior:

1. Set up basic networking game with at least one `GameObject` in a scene that isn't the player.
1. Add a script to the player Prefab that adds parenting to its transform via `gameObject.transform.SetParent()` in the `Start()` method.
1. Launch one instance of the game as Host.
1. Launch another instance and try to join as Client.

## Solution:


If you want to do this when a player has first connected and all `NetworkObjects` (in-scene placed and already dynamically spawned by the server-host) have been fully synchronized with the client then we would recommend using the `NetworkManager.SceneManager.OnSceneEvent` to trap for the `SynchronizeComplete` event.

Here is an example script that we recommend using to achieve this:

```csharp
using Unity.Netcode;

public class ParentPlayerToInSceneNetworkObject : NetworkBehaviour
{   
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Server subscribes to the NetworkSceneManager.OnSceneEvent event
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;

            // Server player is parented under this NetworkObject
            SetPlayerParent(NetworkManager.LocalClientId);
        }
    }

    private void SetPlayerParent(ulong clientId)
    {
        if (IsSpawned && IsServer)
        {
            // As long as the client (player) is in the connected clients list
            if (NetworkManager.ConnectedClients.ContainsKey(clientId))
            {
                // Set the player as a child of this in-scene placed NetworkObject
                // We parent in local space by setting the WorldPositionStays value to false
                NetworkManager.ConnectedClients[clientId].PlayerObject.TrySetParent(NetworkObject, false);
            }
        }
    }

    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        // OnSceneEvent is useful for many things
        switch (sceneEvent.SceneEventType)
        {
            // The SceneEventType event tells the server that a client-player has:
            // 1.) Connected and Spawned
            // 2.) Loaded all scenes that were loaded on the server at the time of connecting
            // 3.) Synchronized (instantiated and spawned) all NetworkObjects in the network session
            case SceneEventType.SynchronizeComplete:
                {
                    // As long as we aren't the server-player
                    if (sceneEvent.ClientId != NetworkManager.LocalClientId)
                    {
                        // Set the newly joined and synchronized client-player as a child of this in-scene placed NetworkObject
                        SetPlayerParent(sceneEvent.ClientId);
                    }
                    break;
                }
        }
    }
}
```

You should place this script on your in-scene placed `NetworkObject` (that is, the first `GameObject`) and do the parenting from it to avoid any timing issues of when it's spawned or the like. It only runs the script on the server-host side since parenting is server authoritative.


> [!NOTE]
> Remove any parenting code you might have had from your player Prefab before using the above script. Depending upon your project's goals, you might be parenting all players under the same in-scene placed `NetworkObject` or you might intend to have each player parenting unique.  If you want each player to be parented under a unique in-scene placed `NetworkObject` then you will need to have the same number of in-scene placed `NetworkObject`s as your maximum allowed players per game session.  The above example will only parent all players under the same in-scene placed `NetworkObject`.  You can extend the above example by migrating the scene event code into an in-scene placed `NetworkObject` that manages the parenting of players (i,e. name it something like `PlayerSpawnManager`) as they connect, make the `SetPlayerParent` method public, and add all in-scene placed `NetworkObject`s to a public list of GameObjects that the `PlayerSpawnManager` will reference and assign player's to as they connect while also freeing in-scene placed `NetworkObject`s as players disconnect during a game session.
