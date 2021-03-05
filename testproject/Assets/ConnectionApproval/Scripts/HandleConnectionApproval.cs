using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Spawning;

public class HandleConnectionApproval : MonoBehaviour
{
    public bool ForgetToSendAuthorizationToken;
    public string ConnectionAuthorization = "this is my secret";
    public Vector3 SpawnPosition;
    public Canvas ButtonCanvas;

    private void Awake()
    {
        Screen.SetResolution(1024, 768, false);
    }

    private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        //Your logic here
        bool approve = false;
        bool createPlayerObject = true;

        if (connectionData != null)
        {
            if (ConnectionAuthorization == System.Text.Encoding.ASCII.GetString(connectionData))
            {
                approve = true;
                Debug.Log("We received a valid connection authorization token! (Validated Connection Approval Works)");
            }
        }
        else
        {
            Debug.Log("We received null as the connection authorization token! (MTT-504 is fixed)");
        }

        //If approve is true, the connection gets added. If it's false. The client gets disconnected
        callback(createPlayerObject, null, approve, SpawnPosition, Quaternion.identity);
    }


    public void ConnectAsHost()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.StartHost();

        if (ButtonCanvas != null)
        {
            ButtonCanvas.gameObject.SetActive(false);
        }
    }

    public void ConnectAsClient()
    {
        if (!ForgetToSendAuthorizationToken)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(ConnectionAuthorization);
        }
        else
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = null;
        }

        NetworkManager.Singleton.StartClient();

        if (ButtonCanvas != null)
        {
            ButtonCanvas.gameObject.SetActive(false);
        }
    }

}
