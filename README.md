# MLAPI
MLAPI (Mid level API) is a framework that hopefully simplifies building networked games in Unity. It is built on the LLAPI and is similar to the HLAPI in many ways. It does not however integrate into the compiler and it's meant to offer much greater flexibility than the HLAPI while keeping some of it's simplicity. It offers greater performance over the HLAPI.

## Features
* Host support (Client hosts the server)
* Object and player spawning \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Object-Spawning)\]
* Connection approval \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Connection-Approval)\]
* Message names
* Replace the integer QOS with names. When you setup the networking you specify names that are associated with a channel. This makes it easier to manage. You can thus specify that a message should be sent on the "damage" channel which handles all damage related logic and is running on the AllCostDelivery channel.
* ProtocolVersion to allow making different versions not talk to each other.
* NetworkedBehaviours does not have to be on the root, it's simply just a class that implements the send methods etc.
* Multiple messages processed every frame with the ability to specify a maximum to prevent freezes in the normal game logic
* Supports separate Unity projects crosstalking
* Passthrough messages \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Passthrough-messages)\]
* Scene Management \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Scene-Management)\]
* Built in Lag compensation \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Lag-Compensation)\]
* NetworkTransform replacement \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/NetworkedTransform)\]
* Targeted messages \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Targeted-Messages)\]
* Port of NetworkedAnimator \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/NetworkedAnimator)\]
* Networked Object Pooling \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Networked-Object-Pooling)\]


## Planned features
* Area of interest
* Encrypted messages / full encryption for all messages. Diffie Hellman key exchange with the option to sign the transaction using RSA.
* Serializer (both for the library to speed up and to allow structs to be sent easily)
* SyncVars (allow variables to automatically be synced to new clients and current clients when it's changed)

_SyncVars will require code injection at compilation with some form of attribute, this is due to limitations of C#. There is no way to have a reference to a variable._
* Message compression

## Example
[Example project](https://github.com/TwoTenPvP/MLAPI-Examples)

The example project has a much lower priority compared to the library itself. If something doesn't exist in the example nor the wiki. Please open an issue on GitHub.



## Issues and missing features
If there are any issues, bugs or features that are missing. Please open an issue on GitHub!
