using System.Text.RegularExpressions;
using MLAPI;
using MLAPI.Transports.UNET;
using UnityEngine;

namespace MLAPI_Examples
{
    public class UnetNetworkingManagerHud : MonoBehaviour
    {
        private int serverPort = -1;
        private int hostPort = -1;
        private int connectPort = -1;
        private string connectAddress;

        private void OnGUI()
        {
            Regex digitsOnly = new Regex(@"[^\d]");

            if (serverPort == -1) serverPort = NetworkingManager.Singleton.NetworkConfig.ConnectPort;
            if (hostPort == -1) hostPort = NetworkingManager.Singleton.NetworkConfig.ConnectPort;
            if (connectPort == -1) connectPort = NetworkingManager.Singleton.NetworkConfig.ConnectPort;
            if (string.IsNullOrEmpty(connectAddress)) connectAddress = NetworkingManager.Singleton.NetworkConfig.ConnectAddress;

            if (!NetworkingManager.Singleton.IsListening)
            {
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Start Host", GUILayout.MinWidth(150)))
                    {
                        UnetTransport.ServerTransports[0].Port = hostPort;
                        NetworkingManager.Singleton.StartHost();
                    }

                    GUILayout.Label("Listen Port:");

                    string stringPort = digitsOnly.Replace(GUILayout.TextField(hostPort.ToString(), GUILayout.MinWidth(50)), "");
                    if (string.IsNullOrEmpty(stringPort)) stringPort = "0";

                    hostPort = Mathf.Clamp(int.Parse(stringPort), 0, ushort.MaxValue);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Start Server", GUILayout.MinWidth(150)))
                    {
                        UnetTransport.ServerTransports[0].Port = serverPort;
                        NetworkingManager.Singleton.StartServer();
                    }

                    GUILayout.Label("Listen Port:");

                    string stringPort = digitsOnly.Replace(GUILayout.TextField(serverPort.ToString(), GUILayout.MinWidth(50)), "");
                    if (string.IsNullOrEmpty(stringPort)) stringPort = "0";

                    serverPort = Mathf.Clamp(int.Parse(stringPort), 0, ushort.MaxValue);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Connect Client", GUILayout.MinWidth(150)))
                    {
                        NetworkingManager.Singleton.NetworkConfig.ConnectPort = connectPort;
                        NetworkingManager.Singleton.NetworkConfig.ConnectAddress = connectAddress;
                        NetworkingManager.Singleton.StartClient();
                    }

                    GUILayout.BeginVertical();
                    {
                        GUILayout.BeginHorizontal();
                        {

                            GUILayout.Label("Connect Port:");

                            string stringPort = digitsOnly.Replace(GUILayout.TextField(connectPort.ToString(), GUILayout.MinWidth(50)), "");
                            if (string.IsNullOrEmpty(stringPort)) stringPort = "0";

                            connectPort = Mathf.Clamp(int.Parse(stringPort), 0, ushort.MaxValue);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Connect Address:");
                            connectAddress = GUILayout.TextField(connectAddress, GUILayout.MinWidth(150));
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
            else
            {
                if (NetworkingManager.Singleton.IsHost)
                {
                    if (GUILayout.Button("Stop Host"))
                    {
                        NetworkingManager.Singleton.StopHost();
                    }
                } 
                else if (NetworkingManager.Singleton.IsServer)
                {
                    if (GUILayout.Button("Stop Server"))
                    {
                        NetworkingManager.Singleton.StopServer();
                    }
                }
                else if (NetworkingManager.Singleton.IsClient)
                {
                    if (GUILayout.Button("Stop Client"))
                    {
                        NetworkingManager.Singleton.StopClient();
                    }
                }
            }
        }
    }
}