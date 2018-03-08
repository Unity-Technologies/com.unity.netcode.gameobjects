![](https://i.imgur.com/m9iGuS9.png)

MLAPI (Mid level API) is a framework that hopefully simplifies building networked games in Unity. It is built on the LLAPI and is similar to the HLAPI in many ways. It does not however integrate into the compiler and it's meant to offer much greater flexibility than the HLAPI while keeping some of it's simplicity. It offers greater performance over the HLAPI.

### Requirements
* Unity 2017 or newer

## Features
* Host support (Client hosts the server)
* Object and player spawning \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Object-Spawning)\]
* Connection approval \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Connection-Approval)\]
* Message names
* Replace the integer QOS with names. When you setup the networking you specify names that are associated with a channel. This makes it easier to manage. You can thus specify that a message should be sent on the "damage" channel which handles all damage related logic and is running on the AllCostDelivery channel.
* ProtocolVersion to allow making different versions not talk to each other.
* NetworkedBehaviours does not have to be on the root, it's simply just a class that implements the send methods etc.
* Custom tickrate
* Supports separate Unity projects crosstalking
* Passthrough messages \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Passthrough-messages)\]
* Scene Management \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Scene-Management)\]
* Built in Lag compensation \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Lag-Compensation)\]
* NetworkTransform replacement \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/NetworkedTransform)\]
* Targeted messages \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Targeted-Messages)\]
* Port of NetworkedAnimator \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/NetworkedAnimator)\]
* Networked Object Pooling \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/Networked-Object-Pooling)\]
* Synced Vars \[[Wiki page](https://github.com/TwoTenPvP/MLAPI/wiki/SyncedVars)\]


## Planned features
* Area of interest
* Encrypted messages / full encryption for all messages. Diffie Hellman key exchange with the option to sign the transaction using RSA.
* Serializer (both for the library to speed up and to allow structs to be sent easily)
* Message compression

## Example
[Example project](https://github.com/TwoTenPvP/MLAPI-Examples)

The example project has a much lower priority compared to the library itself. If something doesn't exist in the example nor the wiki. Please open an issue on GitHub.



## Issues and missing features
If there are any issues, bugs or features that are missing. Please open an issue on GitHub!
## Testing
The project is not extensivley tested. I am however very active on answering and fixing issues. If you are using the library and you find something doesn't work or throws an exception. Open an issue or submit a PR.
