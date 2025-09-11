# Migrate from UNet to Netcode for GameObjects

UNet is deprecated and no longer supported. Follow this guide to migrate from UNet to Netcode for GameObjects.

## Comparison between Netcode for GameObjects and UNet

There are some differences between UNet and Netcode for GameObjects that you should be aware of as part of your migration:

* Naming constraints may cause issues. UNet prefixed methods with `Cmd` or `Rpc`, whereas Netcode requires postfix. This may require either complicated multi-line regex to find and replace, or manual updates. For example, `CommandAttribute` has been replaced with `RpcAttribute(SendTo.Server)` and `ClientRPCAttribute` has been replaced with `RpcAttribute(SendTo.NotServer)` or `RpcAttribute(SendTo.ClientsAndHost)`, depending on whether you want the host client to receive the RPC.
* Errors for RPC postfix naming patterns don't show in your IDE.
* Client and server have separate representations in UNet. UNet has a number of callbacks that don't exist in Netcode for GameObjects.
* Prefabs need to be added to the Prefab registration list in Netcode for GameObjects.
* Matchmaking isn't available in Netcode for GameObjects at this time.

## Back up your project

It's strongly recommended that you back up your existing UNet project before migration. You can do one or both of the following:

* Create a copy of your entire project folder.
* Use source control software, like Git.

## Install Netcode for GameObjects and restart Unity

Follow the [installation guide](install.md) to install Netcode for GameObjects.

Installing Netcode for GameObjects also installs some other packages such as [Unity Transport](https://docs.unity3d.com/Packages/com.unity.transport@latest/), [Unity Collections](https://docs.unity3d.com/Packages/com.unity.collections@latest/), and [Unity Burst](https://docs.unity3d.com/Packages/com.unity.burst@latest/).

## RPC Invoking

Invoking an RPC works the same way as in UNet. Just call the function and it will send an RPC.

<!-- I know a lot of things were done to `NetworkManager` in Netcode and assume this section is just incorrect now.

##  NetworkManager

UNET's `NetworkManager` is also called `NetworkManager` in Netcode and works in a similar way.

:::note
We recommend you don't inherit from `NetworkManager` in Netcode, which was a **recommended** pattern in UNET.
:::

## Replace NetworkManagerHUD

Currently Netcode offers no replacment for the NetworkMangerHUD.

The [Community Contributions Extension Package](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/master/com.community.netcode.extensions) has a a drop in `NetworkManagerHud` component you can use for a quick substitute.
-->

## Replace NetworkIdentity with NetworkObject

UNet's `NetworkIdentity` is called NetworkObject in Netcode for GameObjects and works in a similar way.

## Replace UNet NetworkTransform with Netcode NetworkTransform

UNet's `NetworkTransform` is also called `NetworkTransform` in Netcode for GameObjects and works in a similar way.

`NetworkTransform` doesn't have full feature parity with UNET's `NetworkTransform`. It lacks features like position synchronizing for rigid bodies.

## Replace UNet NetworkAnimator with Netcode NetworkAnimator

Replace UNet's `NetworkAnimator` with Netcode for GameObjects' `NetworkAnimator` component everywhere in your project.

## Update NetworkBehaviour

Replace UNet `NetworkBehaviour` with Netcode for GameObjects' `NetworkBehaviour` everywhere in your project.

```csharp
public class MyUnetClass : NetworkBehaviour
{
    [SyncVar]
    public float MySyncFloat;
    public void Start()
    {
        if (isClient)
        {
            CmdExample(10f);
        }
        else if (isServer)
        {
            RpcExample(10f);
        }
    }
    [Command]
    public void CmdExample(float x)
    {
        Debug.Log(“Runs on server”);
    }
    [ClientRpc]
    public void RpcExample(float x)
    {
        Debug.Log(“Runs on clients”);
    }
}
```

```csharp
public class MyNetcodeExample : NetworkBehaviour
{
    public NetworkVariable<float> MyNetworkVariable = new NetworkVariable<float>();
    public override void OnNetworkSpawn()
    {
        ExampleClientRpc(10f);
        ExampleServerRpc(10f);
    }
    [Rpc(SendTo.Server)]
    public void ExampleServerRpc(float x)
    {
        Debug.Log(“Runs on server”);
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void ExampleClientRpc(float x)
    {
        Debug.Log(“Runs on clients”);
    }
}
```

Refer to [NetworkBehaviour](basics/networkbehaviour.md) for more information.

## Replace SyncVar

Replace `SyncVar` with `NetworkVariable` everywhere in your project.

To achieve equivalent functionality to `SyncVar`, hooks in Netcode for GameObjects subscribe a function to the `OnValueChanged` callback of the `NetworkVariable`. A notable difference between UNet hooks and Netcode for GameObjects' `OnValueChanged` callback is that Netcode for GameObjects gives you both the old and the newly changed value, while UNet only provides you with the old value. With UNet, you also had to manually assign the value of the `SyncVar`.

```csharp
public class SpaceShip : NetworkBehaviour
{
    [SyncVar]
    public string PlayerName;


    [SyncVar(hook = "OnChangeHealth"))]
    public int Health = 42;

    void OnChangeHealth(int newHealth){
        Health = newHealth; //This is no longer necessary in Netcode.
        Debug.Log($"My new health is {newHealth}.");
    }
}
```

```csharp
// Don't forget to initialize NetworkVariable with a value.
public NetworkVariable<string> PlayerName = new NetworkVariable<string>();

public NetworkVariable<int> Health = new NetworkVariable<int>(42);

// This how you update the value of a NetworkVariable, you can also use .Value to access the current value of a NetworkVariable.
void MyUpdate()
{
    Health.Value += 30;
}


void Awake()
{
  // Call this is in Awake or Start to subscribe to changes of the NetworkVariable.
    Health.OnValueChanged += OnChangeHealth;
}

void OnChangeHealth(int oldHealth, int newHealth){
    //There is no need anymore to manually assign the value of the variable here with Netcode. This is done automatically by Netcode for you.
    Debug.Log($"My new health is {newHealth}. Before my health was {oldHealth}");
}
```

Replace all postfix increment and decrement usages of SyncVar in your project. Netcode's `NetworkVariable.Value` exposes a value type that's why postfix increment/decrement isn't supported.

```csharp

public int Health = 42;

public void Update(){
  Health++;
}
```

```csharp

public NetworkVariable<int> Health = new NetworkVariable<int>(42);

public void Update(){
  Health.Value = Health.Value + 1;
}
```

Refer to [NetworkVariable](basics/networkvariable.md) for more information.

## Replace `SyncList` with `NetworkList`

Replace `SyncList<T>` with `NetworkList<T>` everywhere in your project. `NetworkList` has an `OnListChanged` event which is similar to UNet's `Callback`.

```csharp
public SyncListInt m_ints = new SyncListInt();

private void OnIntChanged(SyncListInt.Operation op, int index)
{
    Debug.Log("list changed " + op);
}


public override void OnStartClient()
{
    m_ints.Callback = OnIntChanged;
}
```

```csharp
NetworkList<int> m_ints = new NetworkList<int>();

// Call this is in Awake or Start to subscribe to changes of the NetworkList.
void ListenChanges()
{
    m_ints.OnListChanged += OnIntChanged;
}

// The NetworkListEvent has information about the operation and index of the change.
void OnIntChanged(NetworkListEvent<int> changeEvent)
{

}
```

## Replace Command/ClientRPC

UNet's `Command/ClientRPC` is replaced with `Server/ClientRpc` in Netcode for GameObjects, which works in a similar way.

```csharp
    [Command]
    public void CmdExample(float x)
    {
        Debug.Log(“Runs on server”);
    }
    [ClientRpc]
    public void RpcExample(float x)
    {
        Debug.Log(“Runs on clients”);
    }
```

```csharp
    [Rpc(SendTo.Server)]
    public void ExampleServerRpc(float x)
    {
        Debug.Log(“Runs on server”);
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void ExampleClientRpc(float x)
    {
        Debug.Log(“Runs on clients”);
    }
```

> [!NOTE]
> In Netcode for GameObjects, RPC function names must end with an `Rpc` suffix.

Refer to [Messaging System](advanced-topics/messaging-system.md) for more information.

## Replace `OnServerAddPlayer`

Replace `OnServerAddPlayer` with `ConnectionApproval` everywhere in your project.

```csharp
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

class MyManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader)
    {
        if (extraMessageReader != null)
        {
            var s = extraMessageReader.ReadMessage<StringMessage>();
            Debug.Log("my name is " + s.value);
        }
        OnServerAddPlayer(conn, playerControllerId, extraMessageReader);
    }
}
```

Server-only example:

```csharp
using Unity.Netcode;

private void Setup()
{
    NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    NetworkManager.Singleton.StartHost();
}

private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
{
    //Your logic here
    bool approve = true;
    bool createPlayerObject = true;

    // The Prefab hash. Use null to use the default player prefab
    // If using this hash, replace "MyPrefabHashGenerator" with the name of a Prefab added to the NetworkPrefabs field of your NetworkManager object in the scene
    ulong? prefabHash = NetworkpawnManager.GetPrefabHashFromGenerator("MyPrefabHashGenerator");

    //If approve is true, the connection gets added. If it's false. The client gets disconnected
    callback(createPlayerObject, prefabHash, approve, positionToSpawnAt, rotationToSpawnWith);
}
```

Refer to [Connection Approval](basics/connection-approval.md) for more information.

## Replace NetworkServer.Spawn with NetworkObject.Spawn

Replace `NetworkServer.Spawn`  with `NetworkObject.Spawn` everywhere in your project.

```csharp

using UnityEngine;
using UnityEngine.Networking;

public class Example : NetworkBehaviour
{
    //Assign the Prefab in the Inspector
    public GameObject m_MyGameObject;
    GameObject m_MyInstantiated;

    void Start()
    {
        //Instantiate the prefab
        m_MyInstantiated = Instantiate(m_MyGameObject);
        //Spawn the GameObject you assign in the Inspector
        NetworkServer.Spawn(m_MyInstantiated);
    }
}

```

```csharp
GameObject go = Instantiate(myPrefab, Vector3.zero, Quaternion.identity);
go.GetComponent<NetworkObject>().Spawn();
```

Refer to [Object Spawning](basics/object-spawning.md) for more information.

## Custom Spawn Handlers

Netcode has `Custom Spawn Handlers` to replace UNet's `Custom Spawn Functions`. See [Object Pooling](../advanced-topics/object-pooling) for more information.

## Replace `NetworkContextProperties`

Netcode has `IsLocalPlayer`, `IsClient`, `IsServer`, and `IsHost` to replace UNet's `isLocalPlayer`, `isClient`, and `isServer`. In Netcode for GameObjects, each object can be owned by a specific peer. This can be checked with `IsOwner`, which is similar to UNet's `hasAuthority`.

## `NetworkProximityChecker`, `OnCheckObserver` and network visibility

Netcode for GameObjects doesn't have direct equivalents for the following UNet components and functions:

* `NetworkPromimityChecker` component. Network visibility for clients works similar as in UNet.
* `ObjectHide` message. In Netcode for GameObjects, networked objects on the host are always visible.
* `OnSetLocalVisibility` function. A manual network proximity implementation with the `OnCheckObserver` can be ported to Netcode for GameObjects by using `NetworkObject.CheckObjectVisibility`.
* `OnRebuildObservers` isn't needed for the Netcode for GameObjects' visibility system.

```csharp
public override bool OnCheckObserver(NetworkConnection conn)
{
 return IsvisibleToPlayer(getComponent<NetworkIdentity>(), coon);
}

public bool IsVisibleToPlayer(NetworkIdentity identity, NetworkConnection conn){
    // Any promimity function.
    return true;
}
```

```csharp
public void Start(){
    NetworkObject.CheckObjectVisibility = ((clientId) => {
        return IsVisibleToPlayer(NetworkObject, NetworkManager.Singleton.ConnectedClients[clientId]);
    });
}

public bool IsVisibleToPlayer(NetworkObject networkObject, NetworkClient client){
    // Any promimity function.
    return true;
}
```

Refer to [Object Visibility](basics/object-visibility.md) to learn more about Netcodes network visibility check.

## Update scene management

In Netcode for Gameobjects, scene management isn't done using the `NetworkManager` like in UNet. The `NetworkSceneManager` provides equivalent functionality for switching scenes.

```csharp
public void ChangeScene()
{
    MyNetworkManager.ServerChangeScene("MyNewScene");
}
```

```csharp
public void ChangeScene()
{
    NetworkSceneManager.LoadScene("MyNewScene", LoadSceneMode.Single);
}
```

## Update `ClientAttribute/ClientCallbackAttribute` and `ServerAttribute/ServerCallbackAttribute`

Netcode for GameObjects currently doesn't offer a replacement for marking a function with an attribute so that it only runs on the server or the client. You can manually return out of the function instead.

```csharp
[Client]
public void MyClientOnlyFunction()
{
    Debug.Log("I'm a client!");
}
```

```csharp
public void MyClientOnlyFunction()
{
    if (!IsClient) { return; }

    Debug.Log("I'm a client!");
}
```

## Replace `SyncEvent` with an RPC event

Netcode for GameObjects doesn't provide an equivalent for `SyncEvent`. To port `SyncEvent` code from UNet to Netcode for GameObjects, send an RPC to invoke the event on the other side.

```csharp
public class DamageClass : NetworkBehaviour
{
    public delegate void TakeDamageDelegate(int amount);

    [SyncEvent]
    public event TakeDamageDelegate EventTakeDamage;

    [Command]
    public void CmdTakeDamage(int val)
    {
        EventTakeDamage(val);
    }
}
```

```csharp
public class DamageClass : NetworkBehaviour
{
    public delegate void TakeDamageDelegate(int amount);

    public event TakeDamageDelegate EventTakeDamage;

    [Rpc(SendTo.Server)]
    public void TakeDamageServerRpc(int val)
    {
        EventTakeDamage(val);
        OnTakeDamageClientRpc(val);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void OnTakeDamageClientRpc(int val){
        EventTakeDamage(val);
    }
}
```

## Network Discovery

Netcode for GameObjects doesn't provide Network Discovery. The Contributions repository provides an example implementation for [NetworkDiscovery](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/com.community.netcode.extensions/Runtime/NetworkDiscovery).