# Single player sessions

Netcode for GameObjects provides a [SinglePlayerTransport](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.Transports.SinglePlayer.SinglePlayerTransport.html) which derives from [NetworkTransport](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.NetworkTransport.html).

This provides the ability to run a hosted session using the single player transport without having to modify your primary netcode script.

## Adding the single player transport

- Add the `SinglePlayerTransport` to your NetworkManager.
- You can create a custom `MonoBehaviour` component to handle your connection flow or you can derive from `NetworkManager` and add additional methods/logic to handle starting a single player session or multiplayer session.
  - When starting a single player session, prior to starting the NetworkManager as a host (_required_), you will want to assign the `SinglePlayerTransport` to the `NetworkManager.NetworkConfig.NetworkTransport`.
  - When starting a multiplayer session, prior to starting the NetworkManager, you will want to assign the `UnityTransport` (_or any other `NetworkTransport` derived class that you might use for multiplayer sessions_) to the `NetworkManager.NetworkConfig.NetworkTransport`.

## Example script

Below is an example script that derives from NetworkManager to provide a single method to start a single or multi player session:

```csharp
using Unity.Netcode;
using Unity.Netcode.Transports.SinglePlayer;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Example of how to start a single player or multiplayer session.
/// </summary>
public class ExtendedNetworkManager : NetworkManager
{
    public enum StartType
    {
        SinglePlayer,
        Client,
        Host,
        Server
    }

    private UnityTransport m_UnityTransport;
    private SinglePlayerTransport m_SinglePlayerTransport;

    private void Awake()
    {
        m_UnityTransport = GetComponent<UnityTransport>();
        m_SinglePlayerTransport = GetComponent<SinglePlayerTransport>();
    }

    public bool StartSession(StartType startType)
    {
        var startStatus = false;
        NetworkConfig.NetworkTransport = startType == StartType.SinglePlayer ? m_SinglePlayerTransport : m_UnityTransport;
        switch (startType)
        {
            case StartType.Host:
            case StartType.SinglePlayer:
                {
                    startStatus = StartHost();
                    break;
                }
            case StartType.Server:
                {
                    startStatus = StartServer();
                    break;
                }
            case StartType.Client:
                {
                    startStatus = StartClient();
                    break;
                }
        }
        return startStatus;
    }
}
```
