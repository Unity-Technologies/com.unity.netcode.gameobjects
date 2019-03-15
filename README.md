[![](https://i.imgur.com/d0amtqs.png)](https://midlevel.github.io/MLAPI/)

MLAPI (Mid level API) is a framework that hopefully simplifies building networked games in Unity. It is built on the LLAPI and is similar to the HLAPI in many ways. It does not however integrate into the compiler and it's meant to offer much greater flexibility than the HLAPI while keeping some of it's simplicity. It offers greater performance over the HLAPI.

[![GitHub Release](https://img.shields.io/github/release/MidLevel/MLAPI.svg?logo=github)](https://github.com/MidLevel/MLAPI/releases)
[![Github All Releases](https://img.shields.io/github/downloads/MidLevel/MLAPI/total.svg?logo=github&color=informational)](https://github.com/MidLevel/MLAPI/releases)
[![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=informational)](https://discord.gg/FM8SE9E)
[![Build Status](https://img.shields.io/appveyor/ci/midlevel/mlapi/master.svg?logo=appveyor)](https://ci.appveyor.com/project/MidLevel/mlapi/branch/master)
[![AppVeyor Tests](https://img.shields.io/appveyor/tests/midlevel/mlapi/master.svg?logo=AppVeyor)](https://ci.appveyor.com/project/MidLevel/mlapi/build/tests)


[![Licence](https://img.shields.io/github/license/midlevel/mlapi.svg?color=informational)](https://github.com/MidLevel/MLAPI/blob/master/LICENCE)
[![Website](https://img.shields.io/badge/docs-website-informational.svg)](https://midlevel.github.io/MLAPI/)
[![Wiki](https://img.shields.io/badge/docs-wiki-informational.svg)](https://midlevel.github.io/MLAPI/wiki/)
[![Api](https://img.shields.io/badge/docs-api-informational.svg)](https://midlevel.github.io/MLAPI/api/)

### Documentation
To get started, check the [Wiki](https://midlevel.github.io/MLAPI/).
This is also where most documentation lies.

To get the latest features, the CI server automatically builds the latest commits from master branch. Note that this build still requires the other DLL's. It might be unstable. You can download it [Here](https://ci.appveyor.com/project/MidLevel/mlapi/build/artifacts)

### Support
For bug reports or feature requests you want to propose, please use the Issue Tracker on GitHub. For general questions, networking advice or to discuss changes before proposing them, please use the [Discord server](https://discord.gg/FM8SE9E).

### Requirements
* Unity 2017 or newer
* .NET 4.6 or .NET 3.5 with .NET 2.0 non subset

## Feature highlights
* Host support (Client hosts the server)
* Object and player spawning \[[Wiki page](https://midlevel.github.io/MLAPI/wiki/object-spawning/)\]
* Connection approval \[[Wiki page](https://midlevel.github.io/MLAPI/wiki/connection-approval/)\]
* Strongly Typed RPC Messaging \[[Wiki page](https://midlevel.github.io/MLAPI/wiki/messaging-system/)\]
* Replace the integer QOS with names. When you setup the networking you specify names that are associated with a channel. This makes it easier to manage. You can thus specify that a message should be sent on the "damage" channel which handles all damage related logic and is running on the AllCostDelivery channel.
* ProtocolVersion to allow making different versions not talk to each other.
* NetworkedBehaviours does not have to be on the root, it's simply just a class that implements the send methods etc.
* Custom tickrate
* Synced network time
* Supports separate Unity projects crosstalking
* Scene Management \[[Wiki page](https://midlevel.github.io/MLAPI/wiki/scene-management/)\]
* Built in Lag compensation \[[Wiki page](https://midlevel.github.io/MLAPI/wiki/lag-compensation/)\]
* NetworkTransform replacement
* Port of NetworkedAnimator
* Networked NavMeshAgent
* Networked Object Pooling \[[Wiki page](https://midlevel.github.io/MLAPI/wiki/object-pooling/)\]
* Networked Vars \[[Wiki page](https://midlevel.github.io/MLAPI/wiki/networkedvar/)\]
* Encryption \[[Wiki page](https://midlevel.github.io/MLAPI/wiki/message-encryption/)\]
* Super efficient BitWriter & BitReader \[[Wiki page](https://midlevel.github.io/MLAPI/wiki/bitwriter-bitreader-bitstream/)\]
* Custom UDP transport support \[[Wiki page](https://midlevel.github.io/MLAPI/wiki/custom-transports/)\]
* NetworkProfiler \[[Wiki page](https://midlevel.github.io/MLAPI/wiki/network-profiler-window/)\]

## Special thanks
Special thanks to [Gabriel Tofvesson](https://github.com/GabrielTofvesson) for writing the BitWriter, BitReader & ECDH implementation

## Issues and missing features
If there are any issues, bugs or features that are missing. Please open an issue on the GitHub [issues page](https://github.com/MidLevel/MLAPI/issues)

## Example
[Example project](https://github.com/MidLevel/MLAPI-Examples)

The example project has a much lower priority compared to the library itself. If something doesn't exist in the example nor the wiki. Please open an issue on GitHub.


### Sample Chat
Here is a sample MonoBehaviour showing a chat script where everyone can write and read from.

```csharp
public class Chat : NetworkedBehaviour
{
    private NetworkedList<string> ChatMessages = new NetworkedList<string>(new MLAPI.NetworkedVar.NetworkedVarSettings()
    {
        ReadPermission = MLAPI.NetworkedVar.NetworkedVarPermission.Everyone,
        WritePermission = MLAPI.NetworkedVar.NetworkedVarPermission.Everyone,
        SendTickrate = 5
    }, new List<string>());

    private string textField = "";

    private void OnGUI()
    {
        if (isClient)
        {
            textField = GUILayout.TextField(textField, GUILayout.Width(200));
            if (GUILayout.Button("Send") && !string.IsNullOrWhiteSpace(textField))
            {
                ChatMessages.Add(textField);
                textField = "";
            }

            for (int i = ChatMessages.Count - 1; i >= 0; i--)
            {
                GUILayout.Label(ChatMessages[i]);
            }
        }
    }
}
```