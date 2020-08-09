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

[RequireComponent(typeof(NetworkingManager))]
public class NetworkManagerHud : MonoBehaviour
{
    NetworkingManager netManager;
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
        netManager = GetComponent<NetworkingManager>();
        unetTransport = GetComponent<UnetTransport>();
    }

    private void OnGUI()
    {


            if (!showGUI)
                return;

            GUILayout.BeginArea(new Rect(10 + offsetX, 40 + offsetY, 215, 9999));
            if (!netManager.IsConnectedClient && !netManager.IsServer)
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
            if (!netManager.IsClient)
            {
                // Server + Client
                if (Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    if (GUILayout.Button("Host (Server + Client)"))
                    {
                        netManager.StartHost();
                    }
                }

                // Client + IP
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Client"))
                {
                    netManager.StartClient();
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
                    if (GUILayout.Button("Server Only")) netManager.StartServer();
                }
            }
            else
            {
                // Connecting
                GUILayout.Label("Connecting to " + unetTransport.ConnectAddress + "..");
                if (GUILayout.Button("Cancel Connection Attempt"))
                {
                    netManager.StopClient();
                }
            }
    }

    void StatusLabels()
    {
        // server / client status message
        if (netManager.IsServer)
        {
            GUILayout.Label("Server: active. Transport: " + unetTransport.name);
        }
        if (netManager.IsConnectedClient)
        {
            GUILayout.Label("Client: address=" + unetTransport.ConnectAddress);
        }
    }

    void StopButtons()
    {
        // stop host if host mode
        if (netManager.IsHost && netManager.IsConnectedClient)
        {
            if (GUILayout.Button("Stop Host"))
            {
                netManager.StopHost();
            }
        }
        // stop client if client-only
        else if (netManager.IsConnectedClient)
        {
            if (GUILayout.Button("Stop Client"))
            {
                netManager.StopClient();
            }
        }
        // stop server if server-only
        else if (netManager.IsServer)
        {
            if (GUILayout.Button("Stop Server"))
            {
                netManager.StopServer();
            }
        }
    }
}
```