[![](https://i.imgur.com/d0amtqs.png)](https://midlevel.github.io/MLAPI/)

[![GitHub Release](https://img.shields.io/github/release/MidLevel/MLAPI.svg?logo=github)](https://github.com/MidLevel/MLAPI/releases)
[![NuGet Release](https://img.shields.io/nuget/v/MLAPI.svg?logo=nuget)](https://www.nuget.org/packages/MLAPI/)
[![Github All Releases](https://img.shields.io/github/downloads/MidLevel/MLAPI/total.svg?logo=github&color=informational)](https://github.com/MidLevel/MLAPI/releases)

[![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=informational)](https://discord.gg/FM8SE9E)
[![Build Status](https://img.shields.io/appveyor/ci/midlevel/mlapi/master.svg?logo=appveyor)](https://ci.appveyor.com/project/MidLevel/mlapi/branch/master)
[![AppVeyor Tests](https://img.shields.io/appveyor/tests/midlevel/mlapi/master.svg?logo=AppVeyor)](https://ci.appveyor.com/project/MidLevel/mlapi/build/tests)


[![Licence](https://img.shields.io/github/license/midlevel/mlapi.svg?color=informational)](https://github.com/MidLevel/MLAPI/blob/master/LICENCE)
[![Website](https://img.shields.io/badge/docs-website-informational.svg)](https://midlevel.github.io/MLAPI/)
[![Wiki](https://img.shields.io/badge/docs-wiki-informational.svg)](https://midlevel.github.io/MLAPI/wiki/)
[![Api](https://img.shields.io/badge/docs-api-informational.svg)](https://midlevel.github.io/MLAPI/api/)


MLAPI (Mid level API) is a framework that simplifies building networked games in Unity. It offers **low level** access to core networking while at the same time offering **high level** abstractions. The MLAPI aims to remove the repetetive tasks and reduces the network code dramatically, no matter how many of the **modular** features you use.


The MLAPI has features matched by nobody else, any more features are added when requested. The MLAPI is constantly evolving. Read about our features [here](https://mlapi.network/features/).


### Getting Started
To get started, check the [Wiki](https://mlapi.network/wiki/).
This is also where most documentation lies. Follow the [quickstart](https://mlapi.network/wiki/installation/), join our [discord](http://discord.mlapi.network/) and get started today!

### Support
For bug reports or feature requests you want to propose, please use the Issue Tracker on GitHub. For general questions, networking advice or to discuss changes before proposing them, please use the [Discord server](https://discord.gg/FM8SE9E).

### Compatibility
The MLAPI is built to work everywhere. It will run in the web, on many Unity versions, .NET runtimes and such.

The requirements for the MLAPI are:
* Unity >= 2017

### Special thanks
Special thanks to [Gabriel Tofvesson](https://github.com/GabrielTofvesson) for writing the BitWriter, BitReader & ECDH implementation.

### Issues and missing features
If there are any issues, bugs or features that are missing. Please open an issue on the GitHub [issues page](https://github.com/MidLevel/MLAPI/issues).

### Example
Here is a sample MonoBehaviour showing a chat script where everyone can write and read from. This shows the basis of the MLAPI and the abstractions it adds.

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
        if (IsClient)
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