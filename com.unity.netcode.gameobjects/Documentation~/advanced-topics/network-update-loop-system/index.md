# About NetworkUpdateLoop

Often there is a need to update netcode systems like RPC queue, transport IO, and others outside the standard `MonoBehaviour` event cycle.

The Network Update Loop infrastructure utilizes Unity's low-level Player Loop API allowing for registering `INetworkUpdateSystems` with `NetworkUpdate()` methods to be executed at specific `NetworkUpdateStages` which may be either before or after `MonoBehaviour`-driven game logic execution.

Typically you will interact with `NetworkUpdateLoop` for registration and `INetworkUpdateSystem` for implementation. Systems such as network tick and future features (such as network variable snapshotting) will rely on this pipeline.

## Registration

`NetworkUpdateLoop` exposes four methods for registration:

| Method | Registers |
| -- | -- |
| `void RegisterNetworkUpdate(INetworkUpdateSystem updateSystem, NetworkUpdateStage updateStage)` | Registers an `INetworkUpdateSystem` to be executed on the specified `NetworkUpdateStage` |
| `void RegisterAllNetworkUpdates(INetworkUpdateSystem updateSystem)` | Registers an `INetworkUpdateSystem` to be executed on all `NetworkUpdateStage`s |
| `void UnregisterNetworkUpdate(INetworkUpdateSystem updateSystem, NetworkUpdateStage updateStage)` | Unregisters an `INetworkUpdateSystem` from the specified `NetworkUpdateStage` |
| `void UnregisterAllNetworkUpdates(INetworkUpdateSystem updateSystem)` | Unregisters an `INetworkUpdateSystem` from all `NetworkUpdateStage`s |

## Update Stages

After injection, the player loops follows these stages. The player loop executes the `Initialization` stage and that invokes `NetworkUpdateLoop`'s `RunNetworkInitialization` method which iterates over registered `INetworkUpdateSystems` in `m_Initialization_Array` and calls `INetworkUpdateSystem.NetworkUpdate(UpdateStage)` on them.

In all `NetworkUpdateStages`, it iterates over an array and calls the `NetworkUpdate` method over `INetworkUpdateSystem` interface, and the pattern is repeated.

<Mermaid chart={`
	graph LR;
	A(Initialization)
    B(EarlyUpdate)
    C(FixedUpdate)
    D(PreUpdate)
    E(Update)
    F(PreLateUpdate)
    G(PostLateUpdate)
    A --> B --> C --> D --> E --> F --> G
`}/>

| Stage | Method |
| -- | -- |
| `Initialization` | `RunNetworkInitialization` |
| `EarlyUpdate` | `RunNetworkEarlyUpdate`<br/>`ScriptRunDelayedStartupFrame`<br/>Other systems |
| `FixedUpdate` | `RunNetworkFixedUpdate`<br/>`ScriptRunBehaviourFixedUpdate`<br/>Other systems |
| `PreUpdate` | `RunNetworkPreUpdate`<br/>`PhysicsUpdate`<br/>`Physics2DUpdate`<br/>Other systems |
| `Update` | `RunNetworkUpdate`<br/>`ScriptRunBehaviourUpdate`<br/>Other systems |
| `PreLateUpdate` | `RunNetworkPreLateUpdate`<br/>`ScriptRunBehaviourLateUpdate` |
| `PostLateUpdate` | `PlayerSendFrameComplete`<br/>`RunNetworkPostLateUpdate`<br/>Other systems |

## References

See [Network Update Loop Reference](network-update-loop-reference.md) for process flow diagrams and code.
