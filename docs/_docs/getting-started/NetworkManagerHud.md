---
title: NetworkManagerHUD
permalink: /wiki/NetworkManagerHUD/
---

### USAGE:
This can be added to your NetworkManager Gameobject. Currently its setup for the default Unet Transport Layer.

CODE:
```csharp
using UnityEngine;
using MLAPI;
using MLAPI.Transports.UNET;

public class NetworkManagerHud : MonoBehaviour
{
    UnetTransport unetTransport;

    /// <summary>
    /// Whether to show the default control HUD at runtime.
    /// </summary>
    public bool showGUI = true;

    /// <summary>
    /// The horizontal offset in pixels to draw the HUD runtime GUI at.
    /// </summary>
    public int offsetX;

    /// <summary>
    /// The vertical offset in pixels to draw the HUD runtime GUI at.
    /// </summary>
    public int offsetY;

    private void Start()
    {
        unetTransport = GetComponent<UnetTransport>();
    }

    private void OnGUI()
    {


            if (!showGUI)
                return;

            GUILayout.BeginArea(new Rect(10 + offsetX, 40 + offsetY, 215, 9999));
            if (!NetworkingManager.Singleton.IsConnectedClient && !NetworkingManager.Singleton.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
            }

            StopButtons();

            GUILayout.EndArea();
        }

        void StartButtons()
        {
            if (!NetworkingManager.Singleton.IsClient)
            {
                // Server + Client
                if (Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    if (GUILayout.Button("Host (Server + Client)"))
                    {
                        NetworkingManager.Singleton.StartHost();
                    }
                }

                // Client + IP
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Client"))
                {
                    NetworkingManager.Singleton.StartClient();
                }
                unetTransport.ConnectAddress = GUILayout.TextField(unetTransport.ConnectAddress);
                GUILayout.EndHorizontal();

                // Server Only
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    // cant be a server in webgl build
                    GUILayout.Box("(  WebGL cannot be server  )");
                }
                else
                {
                    if (GUILayout.Button("Server Only")) NetworkingManager.Singleton.StartServer();
                }
            }
            else
            {
                // Connecting
                GUILayout.Label("Connecting to " + unetTransport.ConnectAddress + "..");
                if (GUILayout.Button("Cancel Connection Attempt"))
                {
                NetworkingManager.Singleton.StopClient();
                }
            }
    }

    void StatusLabels()
    {
        // server / client status message
        if (NetworkingManager.Singleton.IsServer)
        {
            GUILayout.Label("Server: active. Transport: " + "UNET");
        }
        if (NetworkingManager.Singleton.IsConnectedClient)
        {
            GUILayout.Label("Client: address=" + unetTransport.ConnectAddress);
        }
    }

    void StopButtons()
    {
        // stop host if host mode
        if (NetworkingManager.Singleton.IsHost && NetworkingManager.Singleton.IsConnectedClient)
        {
            if (GUILayout.Button("Stop Host"))
            {
                NetworkingManager.Singleton.StopHost();
            }
        }
        // stop client if client-only
        else if (NetworkingManager.Singleton.IsConnectedClient)
        {
            if (GUILayout.Button("Stop Client"))
            {
                NetworkingManager.Singleton.StopClient();
            }
        }
        // stop server if server-only
        else if (NetworkingManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Stop Server"))
            {
                NetworkingManager.Singleton.StopServer();
            }
        }
    }
}
```