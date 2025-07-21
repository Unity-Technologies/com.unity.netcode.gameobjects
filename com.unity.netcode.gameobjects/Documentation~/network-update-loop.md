# Network update loop

Understand how network information is updated in Netcode for GameObjects projects.

| **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[About network update loop](advanced-topics/network-update-loop-system/index.md)** | The Network Update Loop infrastructure utilizes Unity's low-level Player Loop API allowing for registering `INetworkUpdateSystems` with `NetworkUpdate()` methods to be executed at specific `NetworkUpdateStages` which may be either before or after `MonoBehaviour`-driven game logic execution. |
| **[Network update loop reference](advanced-topics/network-update-loop-system/network-update-loop-reference.md)** | Diagrams that provide insight into the Network Update Loop process and APIs. |