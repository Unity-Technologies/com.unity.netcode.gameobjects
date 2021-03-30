using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Messaging;

/// <summary>
/// This class is used to verify that the RPC Queue allows for the
/// sending of ClientRpcs to specific clients.  It has several "direct"
/// methods of sending as is defined in the enum ClientRpcDirectTestingModes:
///     Single: This will send to a single client at a time
///     Striped: This will send to alternating client pairs at a time
///     Unified: This sends to all clients at the same time
/// Each direct testing mode sends a count value starting at 0 up to 100.
/// Each direct testing mode sends to all clients at least once during testing (sometimes twice in striped)
/// During direct testing mode the following is also tested:
///     all clients are updating the server with a continually growing counter
///     the server is updating all clients with a global counter
/// During all tests the following additional Rpc Tests are performed:
///     Send client to server no parameters and then multiple parameters
///     Send server to client no parameters and then multiple parameters
/// </summary>
public class ClientCounterBehaviour : NetworkBehaviour
{
    private const float k_ProgressBarDivisor = 1.0f / 200.0f;

    [SerializeField]
    private Text m_CounterTextObject;

    [SerializeField]
    private Image m_ClientProgressBar;

    [SerializeField]
    private GameObject m_ConnectionModeButtonParent;

    private Dictionary<ulong, int> m_ClientSpecificCounters = new Dictionary<ulong, int>();
    private List<ulong> m_ClientIds = new List<ulong>();
    private List<ulong> m_ClientIndices = new List<ulong>();

    private bool m_MultiParameterCanSend;

    private int m_MultiParameterIntValue;
    private int m_MultiParameterValuesCount;
    private int m_MultiParameterNoneCount;
    private int m_RpcMessagesSent;
    private int m_GlobalCounter;
    private int m_GlobalDirectCounter;
    private int m_GlobalDirectCurrentClientIdIndex;
    private int m_LocalClientCounter;
    private int m_GlobalCounterOffset;
    private int m_RpcPerSecond;
    private int m_GlobalDirectScale;

    private long m_MultiParameterLongValue;
    private ulong m_LocalClientId;

    private float m_MultiParameterFloatValue;
    private float m_GlobalCounterDelay;
    private float m_DirectGlobalCounterDelay;
    private float m_LocalCounterDelay;
    private float m_LocalMultiDelay;
    private float m_RpcPerSecondTimer;
    private float m_GlobalDirectFrequency;

    public enum ClientRpcDirectTestingModes
    {
        Single,        //[Tests] sending directly to a single client
        Striped,       //[Tests] sending to multiple alternating clients -- every other client in pairs (i.e. 1 & 3 updated up to 100%, 0 & 2 up to 100%, "repeat")
        Unified,       //[Tests] sending to all clients directly at the same time (same as broadcast but for testing the ability to specify all clients)
    }

    private enum NetworkManagerMode
    {
        Client,
        Host,
        Server,
    }

    private NetworkManagerMode m_CurrentNetworkManagerMode;

    private ClientRpcDirectTestingModes m_ClientRpcDirectTestingMode;

    private ServerRpcParams m_ServerRpcParams;
    private ClientRpcParams m_ClientRpcParams;
    private ClientRpcParams m_ClientRpcParamsMultiParameter;

    private void Start()
    {
        //Start at a smaller resolution until connection mode is selected.
        Screen.SetResolution(320, 320, FullScreenMode.Windowed);
        if (m_CounterTextObject)
        {
            m_CounterTextObject.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Sets the NetworkManager to client mode when invoked via UI button
    /// </summary>
    public void OnCreateClient()
    {
        m_CurrentNetworkManagerMode = NetworkManagerMode.Client;
        InitializeNetworkManager();
    }

    /// <summary>
    /// Sets the NetworkManager to host mode when invoked via UI button
    /// </summary>
    public void OnCreateHost()
    {
        m_CurrentNetworkManagerMode = NetworkManagerMode.Host;
        InitializeNetworkManager();
    }

    /// <summary>
    /// Sets the NetworkManager to server mode when invoked via UI button
    /// </summary>
    public void OnCreateServer()
    {
        m_CurrentNetworkManagerMode = NetworkManagerMode.Server;
        InitializeNetworkManager();
    }

    /// <summary>
    /// Handles common and NetworkManager mode specifc initializations
    /// </summary>
    private void InitializeNetworkManager()
    {
        m_ClientRpcParams.Send.TargetClientIds = new ulong[] { 0 };
        m_ClientRpcParamsMultiParameter.Send.TargetClientIds = new ulong[] { 0 };
        m_ClientRpcDirectTestingMode = ClientRpcDirectTestingModes.Single;
        m_ConnectionModeButtonParent.SetActive(false);
        m_MultiParameterCanSend = true;

        m_GlobalDirectScale = 2;
        m_GlobalDirectFrequency = 1.0f / (100.0f / (float)m_GlobalDirectScale);

        if (m_CounterTextObject)
        {
            m_CounterTextObject.gameObject.SetActive(true);
        }

        switch (m_CurrentNetworkManagerMode)
        {
            case NetworkManagerMode.Client:
                {
                    NetworkManager.Singleton.StartClient();
                    m_ServerRpcParams.Send.UpdateStage = NetworkUpdateStage.Update;
                    Screen.SetResolution(800, 80, FullScreenMode.Windowed);
                    break;
                }
            case NetworkManagerMode.Host:
                {
                    NetworkManager.Singleton.StartHost();
                    m_ClientRpcParams.Send.UpdateStage = NetworkUpdateStage.PreUpdate;
                    Screen.SetResolution(800, 480, FullScreenMode.Windowed);
                    break;
                }
            case NetworkManagerMode.Server:
                {
                    NetworkManager.Singleton.StartServer();
                    m_ClientProgressBar.enabled = false;
                    m_ClientRpcParams.Send.UpdateStage = NetworkUpdateStage.PostLateUpdate;
                    Screen.SetResolution(800, 480, FullScreenMode.Windowed);
                    break;
                }
        }

        m_RpcPerSecondTimer = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// Invoked upon the attached NetworkObject component being initialized by MLAPI
    /// </summary>
    public override void NetworkStart()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            if (IsHost)
            {
                m_ClientSpecificCounters.Add(NetworkManager.Singleton.LocalClientId, 0);
                m_ClientIds.Add(NetworkManager.Singleton.LocalClientId);
            }
        }
    }

    /// <summary>
    /// Unregister for the client connected and disconnected events upon being destroyed
    /// </summary>
    private void OnDestroy()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    /// <param name="clientId">client id that disconnected</param>
    private void OnClientDisconnectCallback(ulong clientId)
    {
        if (m_ClientSpecificCounters.ContainsKey(clientId))
        {
            m_ClientSpecificCounters.Remove(clientId);
        }
        if (m_ClientIds.Contains(clientId))
        {
            m_ClientIds.Remove(clientId);
        }
    }

    /// <summary>
    /// Invoked when a client connects (local client and host-server only receive this message)
    /// </summary>
    /// <param name="clientId">client id that connected</param>
    private void OnClientConnectedCallback(ulong clientId)
    {
        if (IsServer)
        {
            //Exclude the server local id if only a server
            if (!IsHost && clientId == NetworkManager.Singleton.LocalClientId)
            {
                return;
            }
            if (!m_ClientIds.Contains(clientId))
            {
                m_ClientIds.Add(clientId);
            }
            if (!m_ClientSpecificCounters.ContainsKey(clientId))
            {
                m_ClientSpecificCounters.Add(clientId, 0);
            }
        }
    }

    /// <summary>
    /// Both the Server and Client update here
    /// </summary>
    private void Update()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            if (IsServer)
            {
                if (m_ClientSpecificCounters.Count > 0)
                {
                    if (m_GlobalCounterDelay < Time.realtimeSinceStartup)
                    {
                        m_GlobalCounterDelay = Time.realtimeSinceStartup + 0.200f;
                        m_GlobalCounter++;
                        OnSendGlobalCounterClientRpc(m_GlobalCounter);
                        m_RpcMessagesSent++;
                    }

                    if (m_DirectGlobalCounterDelay < Time.realtimeSinceStartup)
                    {
                        switch (m_ClientRpcDirectTestingMode)
                        {
                            case ClientRpcDirectTestingModes.Single:
                                {
                                    SingleDirectUpdate();
                                    break;
                                }
                            case ClientRpcDirectTestingModes.Striped:
                                {
                                    StripedDirectUpdate();
                                    break;
                                }
                            case ClientRpcDirectTestingModes.Unified:
                                {
                                    UnifiedDirectUpdate();
                                    break;
                                }
                        }
                        m_RpcMessagesSent++;
                        m_DirectGlobalCounterDelay = Time.realtimeSinceStartup + m_GlobalDirectFrequency;
                    }
                }
            }

            //Hosts and Clients execute this
            if (IsHost || IsClient)
            {
                if (m_LocalCounterDelay < Time.realtimeSinceStartup)
                {
                    m_LocalCounterDelay = Time.realtimeSinceStartup + 0.25f;
                    m_LocalClientCounter++;
                    OnSendCounterServerRpc(m_LocalClientCounter, m_ServerRpcParams);
                    m_RpcMessagesSent++;
                }
                else if (m_LocalMultiDelay < Time.realtimeSinceStartup)
                {
                    m_LocalMultiDelay = Time.realtimeSinceStartup + 0.325f;
                    if (m_MultiParameterCanSend)
                    {
                        m_MultiParameterCanSend = false;
                        //Multi Parameters
                        OnSendMultiParametersServerRpc(m_MultiParameterIntValue, m_MultiParameterFloatValue, m_MultiParameterLongValue, m_ServerRpcParams);
                        m_RpcMessagesSent++;
                    }
                    else
                    {
                        m_MultiParameterCanSend = true;
                        OnSendNoParametersServerRpc(m_ServerRpcParams);
                        m_RpcMessagesSent++;
                    }
                }
            }

            if (Time.realtimeSinceStartup - m_RpcPerSecondTimer > 1.0f)
            {
                m_RpcPerSecondTimer = Time.realtimeSinceStartup;
                m_RpcPerSecond = m_RpcMessagesSent;
                m_RpcMessagesSent = 0;
            }
        }
    }

    /// <summary>
    /// Cycles through the 3 different ClientRpcDirectTestingModes
    /// </summary>
    private void SelectNextDirectUpdateMethod()
    {
        m_ClientIndices.Clear();
        m_GlobalDirectCounter = 0;
        m_GlobalDirectCurrentClientIdIndex = 0;

        switch (m_ClientRpcDirectTestingMode)
        {
            case ClientRpcDirectTestingModes.Single:
                {
                    m_ClientRpcDirectTestingMode = ClientRpcDirectTestingModes.Striped;
                    break;
                }
            case ClientRpcDirectTestingModes.Striped:
                {
                    //Prepare to send to everyone
                    m_ClientIndices.AddRange(m_ClientIds.ToArray());
                    m_ClientRpcDirectTestingMode = ClientRpcDirectTestingModes.Unified;
                    break;
                }
            case ClientRpcDirectTestingModes.Unified:
                {
                    m_ClientRpcDirectTestingMode = ClientRpcDirectTestingModes.Single;
                    break;
                }
        }
    }

    /// <summary>
    /// Server to Client
    /// Handles Single Direct Updates
    /// </summary>
    private void SingleDirectUpdate()
    {
        if (m_GlobalDirectCounter == 100)
        {
            m_GlobalDirectCurrentClientIdIndex++;
            if (m_GlobalDirectCurrentClientIdIndex >= m_ClientIds.Count)
            {
                SelectNextDirectUpdateMethod();
                return;
            }
            m_GlobalDirectCounter = 0;
            m_ClientIndices.Clear();
        }

        if (m_ClientIndices.Count == 0)
        {
            m_ClientIndices.Add(m_ClientIds[m_GlobalDirectCurrentClientIdIndex]);
        }

        m_ClientRpcParams.Send.TargetClientIds = m_ClientIndices.ToArray();

        m_GlobalDirectCounter = Mathf.Clamp(m_GlobalDirectCounter += m_GlobalDirectScale, 0, 100);

        OnSendDirectCounterClientRpc(m_GlobalDirectCounter, m_ClientRpcParams);
    }

    /// <summary>
    /// Server to Clients
    /// Handles striped direct updates (sends to paired clients)
    /// </summary>
    private void StripedDirectUpdate()
    {
        if (m_GlobalDirectCounter == 100)
        {
            m_GlobalDirectCurrentClientIdIndex++;
            if (m_GlobalDirectCurrentClientIdIndex >= m_ClientIds.Count)
            {
                SelectNextDirectUpdateMethod();
                return;
            }
            m_GlobalDirectCounter = 0;
            m_ClientIndices.Clear();
        }

        if (m_ClientIndices.Count == 0)
        {
            m_ClientIndices.Add(m_ClientIds[m_GlobalDirectCurrentClientIdIndex]);
            var divFactor = (float)m_ClientIds.Count * 0.5f;
            var modFactor = (m_GlobalDirectCurrentClientIdIndex + (int)divFactor) % m_ClientIds.Count;
            m_ClientIndices.Add(m_ClientIds[modFactor]);
        }

        m_ClientRpcParams.Send.TargetClientIds = m_ClientIndices.ToArray();
        m_GlobalDirectCounter = Mathf.Clamp(m_GlobalDirectCounter += m_GlobalDirectScale, 0, 100);

        OnSendDirectCounterClientRpc(m_GlobalDirectCounter, m_ClientRpcParams);
    }

    /// <summary>
    /// Server to Clients
    /// Handles unified direct updates (same as broadcasting, but testing the ability to add all clients)
    /// </summary>
    private void UnifiedDirectUpdate()
    {
        if (m_GlobalDirectCounter == 100)
        {
            SelectNextDirectUpdateMethod();
            return;
        }

        m_ClientRpcParams.Send.TargetClientIds = m_ClientIds.ToArray();
        m_GlobalDirectCounter = Mathf.Clamp(m_GlobalDirectCounter += m_GlobalDirectScale, 0, 100);

        OnSendDirectCounterClientRpc(m_GlobalDirectCounter, m_ClientRpcParams);
    }

    /// <summary>
    /// [Tests] Client to Server
    /// Sends a growing counter (client relative)
    /// </summary>
    /// <param name="counter">the client side counter</param>
    /// <param name="parameters"></param>
    [ServerRpc(RequireOwnership = false)]
    private void OnSendCounterServerRpc(int counter, ServerRpcParams parameters = default)
    {
        //This is just for debug purposes so I can trap for "non-local" clients
        if (IsHost && parameters.Receive.SenderClientId == 0)
        {
            m_ClientSpecificCounters[parameters.Receive.SenderClientId] = counter;
        }
        else if (m_ClientSpecificCounters.ContainsKey(parameters.Receive.SenderClientId))
        {
            m_ClientSpecificCounters[parameters.Receive.SenderClientId] = counter;
        }
    }

    /// <summary>
    /// [Tests] Client to Server
    /// Sends no parameters to the server
    /// </summary>
    /// <param name="parameters"></param>
    [ServerRpc(RequireOwnership = false)]
    private void OnSendNoParametersServerRpc(ServerRpcParams parameters = default)
    {
        m_ClientRpcParamsMultiParameter.Send.TargetClientIds[0] = parameters.Receive.SenderClientId;
        m_ClientRpcParamsMultiParameter.Send.UpdateStage = NetworkUpdateStage.Update;
        OnSendNoParametersClientRpc(m_ClientRpcParamsMultiParameter);
    }

    /// <summary>
    /// [Tests] Client to Server
    /// Sends multiple parameters to the server
    /// </summary>
    /// <param name="parameters"></param>
    [ServerRpc(RequireOwnership = false)]
    private void OnSendMultiParametersServerRpc(int count, float floatValue, long longValue, ServerRpcParams parameters = default)
    {
        m_ClientRpcParamsMultiParameter.Send.TargetClientIds[0] = parameters.Receive.SenderClientId;
        m_ClientRpcParamsMultiParameter.Send.UpdateStage = NetworkUpdateStage.EarlyUpdate;
        OnSendMultiParametersClientRpc(count, floatValue, longValue, m_ClientRpcParamsMultiParameter);
    }

    /// <summary>
    /// [Tests] Server to Client
    /// Sends no parameters to the server
    /// </summary>
    /// <param name="parameters"></param>
    [ClientRpc]
    private void OnSendNoParametersClientRpc(ClientRpcParams parameters = default)
    {
        m_MultiParameterNoneCount++;
    }


    /// <summary>
    /// [Tests] Server to Client
    /// Sends multiple parameters to the server
    /// </summary>
    /// <param name="parameters"></param>
    [ClientRpc]
    private void OnSendMultiParametersClientRpc(int count, float floatValue, long longValue, ClientRpcParams parameters = default)
    {
        if (m_MultiParameterIntValue == count && floatValue == m_MultiParameterFloatValue && m_MultiParameterLongValue == longValue)
        {
            m_MultiParameterValuesCount++;
        }
        else
        {
            m_MultiParameterValuesCount--;
        }
        m_MultiParameterIntValue = Random.Range(0, 100);
        m_MultiParameterFloatValue = Random.Range(0.0f, 1.0f);
        m_MultiParameterLongValue = (long)Random.Range(0, 10000);
    }

    /// <summary>
    /// [Tests] Server to Clients
    /// [Tests] broadcasting to all clients (similar to unified direct without specifying all client ids)
    /// </summary>
    /// <param name="counter">the global counter value</param>
    [ClientRpc]
    private void OnSendGlobalCounterClientRpc(int counter)
    {
        m_GlobalCounter = counter;
        if (m_GlobalCounterOffset == 0)
        {
            m_GlobalCounterOffset = Mathf.Max(counter - 1, 0);
        }
    }

    /// <summary>
    /// Server to Client
    /// Handles setting the m_GlobalDirectCounter for the client in questions
    /// [Tests] Sending to random pairs of clients (i.e. multi-client but not broadcast)
    /// </summary>
    /// <param name="counter"></param>
    /// <param name="parameters"></param>
    [ClientRpc]
    private void OnSendDirectCounterClientRpc(int counter, ClientRpcParams parameters = default)
    {
        m_GlobalDirectCounter = counter;
    }

    /// <summary>
    /// Update either the client or server display information
    /// </summary>
    private void OnGUI()
    {
        if (m_CounterTextObject)
        {
            if (IsServer)
            {
                UpdateServerInfo();
            }
            else
            {
                UpdateClientInfo();
            }
        }
    }

    /// <summary>
    /// Update the client text info and progress bar
    /// </summary>
    private void UpdateClientInfo()
    {
        if (m_LocalClientId == 0 && NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
        {
            m_LocalClientId = NetworkManager.Singleton.LocalClientId;
        }

        m_CounterTextObject.text = $"Client-ID [{m_LocalClientId}]  Broadcast Rpcs Received:  {m_GlobalCounter - m_GlobalCounterOffset}  |  Direct Rpcs Received: {m_GlobalDirectCounter} \n";
        m_CounterTextObject.text += $"{nameof(m_MultiParameterValuesCount)} : {m_MultiParameterValuesCount}  |  {nameof(m_MultiParameterNoneCount)} : {m_MultiParameterNoneCount}";

        if (m_ClientProgressBar)
        {
            m_ClientProgressBar.fillAmount = Mathf.Clamp((2.0f * (float)m_GlobalDirectCounter) * k_ProgressBarDivisor, 0.01f, 1.0f);
        }
    }

    /// <summary>
    /// Updates the server text info and host progress bar
    /// </summary>
    private void UpdateServerInfo()
    {
        string updatedCounters = string.Empty;
        foreach (var entry in m_ClientSpecificCounters)
        {
            if (entry.Key == 0 && IsHost)
            {
                updatedCounters += $"Client-ID [{entry.Key}]  Client to Server Rpcs Received: {entry.Value}  |  Broadcast Rpcs Sent:{m_GlobalCounter} -- Direct Rpcs Sent:{m_GlobalDirectCounter}\n";
                updatedCounters += $"{nameof(m_MultiParameterValuesCount)} : {m_MultiParameterValuesCount}  |  {nameof(m_MultiParameterNoneCount)} : {m_MultiParameterNoneCount}\n";
                updatedCounters += $"{nameof(m_RpcPerSecond)} : {m_RpcPerSecond}\n ";
            }
            else
            {
                updatedCounters += $"Client-ID [{entry.Key}]  Client to Server Rpcs Received: {entry.Value}\n";
            }
        }

        updatedCounters += $"{nameof(m_ClientRpcDirectTestingMode)} : {m_ClientRpcDirectTestingMode}";

        if (IsHost)
        {
            if (m_GlobalDirectCurrentClientIdIndex < m_ClientIds.Count)
            {
                if (m_ClientProgressBar && m_ClientIndices.Contains(NetworkManager.Singleton.LocalClientId))
                {
                    m_ClientProgressBar.fillAmount = Mathf.Clamp((2.0f * (float)m_GlobalDirectCounter) * k_ProgressBarDivisor, 0.01f, 1.0f);
                }
            }
        }

        m_CounterTextObject.text = updatedCounters;
    }
}
