# Addressable Scene Loading

The `NetworkSceneManager` can load, unload, and synchronize scenes that are delivered through the [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest) system in addition to scenes registered in your project's [build settings scenes in build list](https://docs.unity3d.com/Manual/BuildSettings.html).

Addressable scenes work **alongside** build-settings scenes: within the same session you can load some scenes the traditional way (`LoadScene(string sceneName, ...)`) and others as Addressables. All of the integrated scene management features apply equally to both, including:
- Automatic client synchronization (including late joiners).
- Scene events and scene event progress tracking (`OnSceneEvent`, `SceneEventType.*`).
- In-scene placed `NetworkObject` synchronization.
- Scene validation (`VerifySceneBeforeLoading`).
- Single and additive load modes.

> [!NOTE]
> Addressable scene support requires the [`com.unity.addressables`](https://docs.unity3d.com/Packages/com.unity.addressables@latest) package to be present in your project. Without it, the `AssetReference`-based overloads (`LoadScene(AssetReference, ...)` and `RegisterAddressableScene(AssetReference)`) are compiled out, and the address-based APIs (`LoadAddressableScene`, `RegisterAddressableScene(string)`, `EnableAddressableSceneAutoScan`) throw a `NotSupportedException` if used — only build-settings scenes can be loaded.

## How it works

Netcode identifies every scene on the wire by a `uint` hash. For build-settings scenes, that hash is derived from the scene's path. For Addressable scenes, the same hashing algorithm is applied to the scene's **address** (its Addressable key). This means Addressable scenes travel over the network using the exact same message format as build-settings scenes — there's no separate wire protocol.

The hash is derived from the address. The server (or session owner) is the source of truth: every scene event it sends carries its full registered `hash → address` table in the message (the table is empty, and therefore free, for projects with no registered Addressable scenes). Clients register those mappings automatically as they arrive, so **clients don't need to register Addressable scenes themselves** — they just need the referenced address to exist in their own Addressables content catalog so it can be loaded.

> [!NOTE]
> A scene must be marked as **Addressable** and included in your Addressables build (or Play Mode content) on every peer that needs to load it. A scene that's only in the build settings scene list is loaded with the traditional path, not through Addressables. When all peers share the same Addressables content (the normal case for a shared build), every address the server references will resolve on the clients.

## Registering Addressable scenes

The server needs to know an Addressable scene's address before it can load it. The simplest path is to just call `LoadAddressableScene` / `LoadScene(AssetReference, ...)` (see below) — they auto-register the address for you. The registration APIs below exist for cases where you want to register up front (for example, to validate addresses or to network scenes you didn't load through those overloads).

> [!NOTE]
> Clients do not need to call any of these registration APIs. The server communicates the `hash → address` mapping over the wire for every scene event, so clients resolve Addressable scenes automatically. Registration is only relevant on the side that initiates the load (server / session owner).

### Automatic catalog scan

Enable auto-scan **before** starting the `NetworkManager`. When the `NetworkSceneManager` is created (as the `NetworkManager` starts), it scans the Addressables content catalog for every scene resource location and registers each of their addresses:

```csharp
// Call before NetworkManager.StartServer/StartHost/StartClient
NetworkSceneManager.EnableAddressableSceneAutoScan(true);
```

This makes every Addressable scene in the catalog networkable with no per-scene setup.

### Explicit registration

You can also register scenes individually, for deterministic control over exactly which addresses are networkable:

```csharp
// By address string:
NetworkSceneManager.RegisterAddressableScene("Assets/Scenes/MyAddressableScene.unity");

// By AssetReference (uses its runtime key):
NetworkSceneManager.RegisterAddressableScene(myAssetReference);
```

`RegisterAddressableScene` is **static and persistent**: registrations are remembered across `NetworkManager` sessions and are automatically applied to every `NetworkSceneManager` when it starts, so you can register once in your bootstrap code before starting the `NetworkManager`. It is idempotent, so it's safe to call repeatedly with the same address.

## Loading an Addressable scene

Loading is server/session-owner-only, just like build-settings scenes. Use one of the Addressable load overloads instead of `LoadScene(string, LoadSceneMode)`:

```csharp
// By address string:
var status = NetworkManager.SceneManager.LoadAddressableScene(
    "Assets/Scenes/MyAddressableScene.unity", LoadSceneMode.Additive);

// By AssetReference:
var status = NetworkManager.SceneManager.LoadScene(myAssetReference, LoadSceneMode.Additive);

if (status != SceneEventProgressStatus.Started)
{
    Debug.LogWarning($"Failed to load Addressable scene with a {nameof(SceneEventProgressStatus)}: {status}");
}
```

Both overloads auto-register the address if it hasn't been registered yet. The returned [`SceneEventProgressStatus`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.SceneEventProgressStatus.html) behaves exactly as it does for build-settings scenes (see [Scene Event Progress Status](using-networkscenemanager.md#scene-event-progress-status)).

### Example

The following builds on the [`ProjectSceneManager` example](using-networkscenemanager.md#basic-scene-loading-example), loading an Addressable scene instead of a build-settings scene:

```csharp
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class ProjectAddressableSceneManager : NetworkBehaviour
{
    // Assign a scene AssetReference in the inspector.
    [SerializeField]
    private AssetReference m_SceneReference;

    private Scene m_LoadedScene;

    public override void OnNetworkSpawn()
    {
        if (IsServer && m_SceneReference != null && m_SceneReference.RuntimeKeyIsValid())
        {
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;

            var status = NetworkManager.SceneManager.LoadScene(m_SceneReference, LoadSceneMode.Additive);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogWarning($"Failed to load Addressable scene with a {nameof(SceneEventProgressStatus)}: {status}");
            }
        }

        base.OnNetworkSpawn();
    }

    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        // Store the loaded scene on the server so it can be unloaded later.
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete
            && sceneEvent.ClientId == NetworkManager.ServerClientId)
        {
            m_LoadedScene = sceneEvent.Scene;
        }
    }
}
```

## Unloading an Addressable scene

Unloading is unified — use the same `NetworkSceneManager.UnloadScene(Scene)` method for both build-settings and Addressable scenes:

```csharp
if (m_LoadedScene.IsValid() && m_LoadedScene.isLoaded)
{
    var status = NetworkManager.SceneManager.UnloadScene(m_LoadedScene);
}
```

The `NetworkSceneManager` tracks how each scene was loaded and automatically routes the unload through Addressables when appropriate, releasing the underlying Addressables handle for you. Only additively loaded scenes can be unloaded (see [Unloading a Scene](using-networkscenemanager.md#unloading-a-scene)).

## Scene validation

Addressable scenes participate in scene validation the same way build-settings scenes do. When the `VerifySceneBeforeLoading` delegate is invoked for an Addressable scene, the scene index is `-1` (Addressable scenes have no build index) and the scene name is the **address**:

```csharp
private bool ServerSideSceneValidation(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
{
    // sceneIndex is -1 for Addressable scenes; sceneName is the address.
    if (sceneIndex == -1 && sceneName == "Assets/Scenes/RestrictedAddressableScene.unity")
    {
        return false;
    }
    return true;
}
```

See [Scene Validation](using-networkscenemanager.md#scene-validation) for more details.

## Things to be aware of

- **`SceneEvent.AsyncOperation` is `null` for Addressable scenes.** An Addressables load doesn't expose a `UnityEngine.AsyncOperation`, so this field isn't populated for Addressable scene events. Netcode still tracks load/unload completion internally and fires all of the normal scene event notifications. If you need progress or completion, rely on the scene event notifications (for example `SceneEventType.LoadComplete` / `SceneEventType.LoadEventCompleted`) rather than the `AsyncOperation`.
- **Clients must have the referenced content in their catalog.** The server sends the address to clients automatically, but each client still loads the scene through its own Addressables system, so the address must resolve in the client's content catalog (normally guaranteed by a shared build). Clients do not need to pre-register addresses.
- **The address string must be consistent on the loading side.** The wire hash is derived from the exact address you load with, so use the same address/`AssetReference` your Addressables content actually uses.

## See also

- [Using NetworkSceneManager](using-networkscenemanager.md)
- [Scene events](scene-events.md)
- [Client synchronization mode](client-synchronization-mode.md)
