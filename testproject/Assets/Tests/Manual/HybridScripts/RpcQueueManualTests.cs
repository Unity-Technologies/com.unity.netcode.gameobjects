using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace TestProject.ManualTests
{
    /// <summary>
    /// This class is used to verify that the RPC Queue allows for the
    /// sending of ClientRpcs to specific clients.  It has several "direct"
    /// methods of sending as is defined in the ENUM ClientRpcDirectTestingModes:
    ///     Single: This will send to a single client at a time
    ///     Striped: This will send to alternating client pairs at a time
    ///     Unified: This sends to all clients at the same time
    /// Each direct testing mode sends a count value starting at 0 up to 100.
    /// Each direct testing mode sends to all clients at least once during testing (sometimes twice in striped)
    /// During direct testing mode the following is also tested:
    ///     all clients are updating the server with a continually growing counter
    ///     the server is updating all clients with a global counter
    /// During all tests the following additional RPC Tests are performed:
    ///     Send client to server no parameters and then multiple parameters
    ///     Send server to client no parameters and then multiple parameters
    /// </summary>
    public class RpcQueueManualTests : NetworkBehaviour
    {
        public static bool UnitTesting;

        private const float k_ProgressBarDivisor = 1.0f / 200.0f;
        [SerializeField]
        private bool m_RunInTestMode;

        [SerializeField]
        [Range(1, 10)]
        private int m_IterationsToRun;

        [SerializeField]
        private Text m_CounterTextObject;

        [SerializeField]
        private Image m_ClientProgressBar;

        [SerializeField]
        private GameObject m_ConnectionModeButtonParent;

        [SerializeField]
        private NetworkManager m_ManualTestNetworkManager;

        private Dictionary<ulong, int> m_ClientSpecificCounters = new Dictionary<ulong, int>();
        private List<ulong> m_ClientIds = new List<ulong>();
        private List<ulong> m_ClientIndices = new List<ulong>();


        private bool m_BeginTest;
        private bool m_HasBeenInitialized;
        private bool m_ContinueToRun;
        private bool m_ConnectionEventOccurred;
        private bool m_MultiParameterCanSend;


        private int m_MaxGlobalDirectCounter;
        private int m_TotalIterations;
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

        private float m_MesageSendDelay;
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


        public void DisableManualNetworkManager()
        {
            if (m_ManualTestNetworkManager)
            {
                m_ManualTestNetworkManager.gameObject.SetActive(false);
            }
        }

        public bool IsFinishedWithTest()
        {
            if (m_RunInTestMode)
            {
                return !m_ContinueToRun;
            }
            return false;
        }

        public void BeginTest()
        {
            m_BeginTest = true;
        }

        public void SetTestingMode(bool enabled, int iterationCount)
        {
            m_ContinueToRun = true;
            m_RunInTestMode = enabled;
            m_IterationsToRun = Mathf.Clamp(iterationCount, 1, 10);
        }

        public string GetCurrentServerStatusInfo()
        {
            return m_ServerUpdateInfo;
        }

        public string GetCurrentClientStatusInfo()
        {
            return m_ClientUpdateInfo;
        }

        private void Start()
        {
            m_ContinueToRun = true;
            if (!UnitTesting)
            {
                m_BeginTest = true;
                Initialize();
                m_MaxGlobalDirectCounter = 100;
                m_MesageSendDelay = 0.20f;
            }
            else
            {
                m_ClientRpcParams.Send.TargetClientIds = new ulong[] { 0 };
                m_ClientRpcParamsMultiParameter.Send.TargetClientIds = new ulong[] { 0 };
                //For unit tests we will only send 2 per update stage
                m_MaxGlobalDirectCounter = 2;
                m_BeginTest = false;
                m_MesageSendDelay = 0.01f;

                var gameObject = GameObject.Find("[NetworkManager]");
                if (gameObject != null)
                {
                    gameObject.SetActive(false);
                    Destroy(gameObject);
                    Debug.Log($"Found scene {nameof(NetworkManager)}, disabled it, and destroyed it.");
                }
            }
        }

        private void Initialize()
        {
            m_TotalIterations = 0;
            //Start at a smaller resolution until connection mode is selected.
            Screen.SetResolution(320, 320, FullScreenMode.Windowed);
            if (m_CounterTextObject)
            {
                m_CounterTextObject.gameObject.SetActive(false);
            }
            if (m_ConnectionModeButtonParent)
            {
                var connectionModeScript = m_ConnectionModeButtonParent.GetComponent<ConnectionModeScript>();
                if (connectionModeScript)
                {
                    connectionModeScript.OnNotifyConnectionEventClient += OnNotifyConnectionEventClient;
                    connectionModeScript.OnNotifyConnectionEventHost += OnNotifyConnectionEventHost;
                    connectionModeScript.OnNotifyConnectionEventServer += OnNotifyConnectionEventServer;
                }
            }

        }

        private void OnNotifyConnectionEventServer()
        {
            m_ConnectionEventOccurred = true;
            OnCreateServer();
        }

        private void OnNotifyConnectionEventHost()
        {
            m_ConnectionEventOccurred = true;
            OnCreateHost();
        }

        private void OnNotifyConnectionEventClient()
        {
            m_ConnectionEventOccurred = true;
            OnCreateClient();
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
        /// Handles common and NetworkManager mode specific initializations
        /// </summary>
        private void InitializeNetworkManager()
        {
            m_ClientRpcParams.Send.TargetClientIds = new ulong[] { 0 };
            m_ClientRpcParamsMultiParameter.Send.TargetClientIds = new ulong[] { 0 };

            m_ClientRpcDirectTestingMode = ClientRpcDirectTestingModes.Single;
            if (m_ConnectionModeButtonParent)
            {
                m_ConnectionModeButtonParent.SetActive(false);
            }
            m_MultiParameterCanSend = true;

            if (!UnitTesting)
            {
                m_GlobalDirectScale = 2;
                m_GlobalDirectFrequency = 1.0f / (100.0f / m_GlobalDirectScale);
            }
            else
            {
                m_GlobalDirectScale = 1;
                m_GlobalDirectFrequency = 0.01f;
            }

            if (m_CounterTextObject)
            {
                m_CounterTextObject.gameObject.SetActive(true);
            }

            switch (m_CurrentNetworkManagerMode)
            {
                case NetworkManagerMode.Client:
                    {
                        if (!m_ConnectionEventOccurred && !UnitTesting)
                        {
                            NetworkManager.StartClient();
                            Screen.SetResolution(800, 80, FullScreenMode.Windowed);
                        }

                        break;
                    }
                case NetworkManagerMode.Host:
                    {
                        if (!m_ConnectionEventOccurred && !UnitTesting)
                        {
                            NetworkManager.StartHost();
                            Screen.SetResolution(800, 480, FullScreenMode.Windowed);
                        }

                        break;
                    }
                case NetworkManagerMode.Server:
                    {
                        if (!m_ConnectionEventOccurred && !UnitTesting)
                        {
                            NetworkManager.StartServer();
                            Screen.SetResolution(800, 480, FullScreenMode.Windowed);
                            m_ClientProgressBar.enabled = false;
                        }

                        break;
                    }
            }

            m_RpcPerSecondTimer = Time.realtimeSinceStartup;
            m_HasBeenInitialized = true;
        }

        /// <summary>
        /// Invoked upon the attached NetworkObject component being initialized by the netcode
        /// </summary>
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
                if (IsHost)
                {
                    m_ClientSpecificCounters.Add(NetworkManager.LocalClientId, 0);
                    m_ClientIds.Add(NetworkManager.LocalClientId);
                }
            }
        }

        /// <summary>
        /// Unregister for the client connected and disconnected events upon being despawned
        /// </summary>
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
            base.OnNetworkDespawn();
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
                if (!IsHost && clientId == NetworkManager.LocalClientId)
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
            if (NetworkManager != null && NetworkManager.IsListening && ((IsServer && NetworkManager.ConnectedClientsList.Count > 1) || IsClient) && m_ContinueToRun && m_BeginTest)
            {
                if (UnitTesting && !m_HasBeenInitialized)
                {
                    if (IsClient)
                    {
                        OnCreateClient();
                    }
                    else
                    {
                        OnCreateHost();
                    }
                    return;
                }

                if (m_RunInTestMode && m_IterationsToRun <= m_TotalIterations)
                {
                    m_ContinueToRun = false;
                }
                else
                {
                    if (IsServer)
                    {
                        if (m_ClientSpecificCounters.Count > 0)
                        {
                            if (m_GlobalCounterDelay < Time.realtimeSinceStartup)
                            {
                                if (!UnitTesting)
                                {
                                    m_GlobalCounterDelay = Time.realtimeSinceStartup + 0.2f;
                                }
                                else
                                {
                                    m_GlobalCounterDelay = Time.realtimeSinceStartup + 0.06f;
                                }
                                m_GlobalCounter++;
                                OnSendGlobalCounterClientRpc(m_GlobalCounter);
                                OnSendGlobalCounterClientRpc((float)m_GlobalCounter);
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
                            m_LocalCounterDelay = Time.realtimeSinceStartup + m_MesageSendDelay + 0.02f;
                            m_LocalClientCounter++;

                            OnSendCounterServerRpc(m_LocalClientCounter, NetworkManager.LocalClientId, m_ServerRpcParams);
                            m_RpcMessagesSent++;
                        }
                        else if (m_LocalMultiDelay < Time.realtimeSinceStartup)
                        {
                            m_LocalMultiDelay = Time.realtimeSinceStartup + m_MesageSendDelay + 0.03f;
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
                        if (m_RunInTestMode)
                        {
                            m_TotalIterations++;
                        }
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
            if (m_GlobalDirectCounter == m_MaxGlobalDirectCounter)
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

            m_GlobalDirectCounter = Mathf.Clamp(m_GlobalDirectCounter += m_GlobalDirectScale, 0, m_MaxGlobalDirectCounter);

            OnSendDirectCounterClientRpc(m_GlobalDirectCounter, m_ClientRpcParams);
            m_ServerDirectTotalRpcCount++;
        }

        /// <summary>
        /// Server to Clients
        /// Handles striped direct updates (sends to paired clients)
        /// </summary>
        private void StripedDirectUpdate()
        {
            if (m_GlobalDirectCounter == m_MaxGlobalDirectCounter)
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
                var divFactor = m_ClientIds.Count * 0.5f;
                var modFactor = (m_GlobalDirectCurrentClientIdIndex + (int)divFactor) % m_ClientIds.Count;
                m_ClientIndices.Add(m_ClientIds[modFactor]);
            }

            m_ClientRpcParams.Send.TargetClientIds = m_ClientIndices.ToArray();
            m_GlobalDirectCounter = Mathf.Clamp(m_GlobalDirectCounter += m_GlobalDirectScale, 0, m_MaxGlobalDirectCounter);

            OnSendDirectCounterClientRpc(m_GlobalDirectCounter, m_ClientRpcParams);
            m_ServerDirectTotalRpcCount += m_ClientIndices.Count;
        }

        /// <summary>
        /// Server to Clients
        /// Handles unified direct updates (same as broadcasting, but testing the ability to add all clients)
        /// </summary>
        private void UnifiedDirectUpdate()
        {
            if (m_GlobalDirectCounter == m_MaxGlobalDirectCounter)
            {
                SelectNextDirectUpdateMethod();
                return;
            }

            m_ClientRpcParams.Send.TargetClientIds = m_ClientIds.ToArray();
            m_GlobalDirectCounter = Mathf.Clamp(m_GlobalDirectCounter += m_GlobalDirectScale, 0, m_MaxGlobalDirectCounter);
            OnSendDirectCounterClientRpc(m_GlobalDirectCounter, m_ClientRpcParams);
            m_ServerDirectTotalRpcCount += m_ClientIds.Count;
        }

        /// <summary>
        /// [Tests] Client to Server
        /// Sends a growing counter (client relative)
        /// </summary>
        /// <param name="counter">the client side counter</param>
        /// <param name="parameters"></param>
        [ServerRpc(RequireOwnership = false)]
        private void OnSendCounterServerRpc(int counter, ulong clientId, ServerRpcParams parameters = default)
        {
            //This is just for debug purposes so I can trap for "non-local" clients
            if (m_ClientSpecificCounters.ContainsKey(parameters.Receive.SenderClientId))
            {
                if (m_ClientSpecificCounters[parameters.Receive.SenderClientId] < counter)
                {
                    m_ClientSpecificCounters[parameters.Receive.SenderClientId] = counter;
                }
                else
                {
                    Debug.LogWarning($"Client counter was sent {counter} value but it already was at a value of {m_ClientSpecificCounters[parameters.Receive.SenderClientId]}");
                }
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
            m_ClientRpcParamsMultiParameter.Send.TargetClientIds = new[] { parameters.Receive.SenderClientId };
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
            m_ClientRpcParamsMultiParameter.Send.TargetClientIds = new[] { parameters.Receive.SenderClientId };
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
        /// Sends multiple parameters to the client
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
            m_MultiParameterLongValue = Random.Range(0, 10000);
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
        /// [Tests] Server to Clients
        /// [Tests] broadcasting to all clients (similar to unified direct without specifying all client ids)
        /// </summary>
        /// <param name="counter">the global counter value</param>
        [ClientRpc]
        private void OnSendGlobalCounterClientRpc(float counter)
        {
            m_GlobalCounter = (int)counter;
            if (m_GlobalCounterOffset == 0)
            {
                m_GlobalCounterOffset = Mathf.Max(m_GlobalCounter - 1, 0);
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
            m_ClientDirectTotalRpcCount++;
        }

        /// <summary>
        /// Update either the client or server display information
        /// </summary>
        private void OnGUI()
        {
            if (IsServer && !IsHost || (IsHost && !UnitTesting))
            {
                UpdateServerInfo();
            }
            else if (IsHost && UnitTesting)
            {
                UpdateServerInfo();
                UpdateClientInfo();
            }
            else
            {
                UpdateClientInfo();
            }
        }

        private string m_ClientUpdateInfo;
        private int m_ClientDirectTotalRpcCount;

        /// <summary>
        /// Update the client text info and progress bar
        /// </summary>
        private void UpdateClientInfo()
        {
            if (m_LocalClientId == 0 && NetworkManager && NetworkManager.IsListening)
            {
                m_LocalClientId = NetworkManager.LocalClientId;
            }
            m_ClientUpdateInfo = $"Client-ID [{m_LocalClientId}]  Broadcast Rpcs Received:  {m_GlobalCounter - m_GlobalCounterOffset}  |  Direct Rpcs Received: {(UnitTesting ? m_ClientDirectTotalRpcCount : m_GlobalDirectCounter)} \n";
            m_ClientUpdateInfo += $"{nameof(m_MultiParameterValuesCount)} : {m_MultiParameterValuesCount}  |  {nameof(m_MultiParameterNoneCount)} : {m_MultiParameterNoneCount}";

            if (!UnitTesting)
            {
                m_CounterTextObject.text = m_ClientUpdateInfo;
                if (m_ClientProgressBar)
                {
                    m_ClientProgressBar.fillAmount = Mathf.Clamp((2.0f * m_GlobalDirectCounter) * k_ProgressBarDivisor, 0.01f, 1.0f);
                }
            }
        }

        private string m_ServerUpdateInfo;
        private int m_ServerDirectTotalRpcCount;
        /// <summary>
        /// Updates the server text info and host progress bar
        /// </summary>
        private void UpdateServerInfo()
        {
            m_ServerUpdateInfo = string.Empty;
            foreach (var entry in m_ClientSpecificCounters)
            {
                if (entry.Key == NetworkManager.LocalClientId && IsHost)
                {
                    m_ServerUpdateInfo += $"Client-ID [{entry.Key}]  Client to Server Rpcs Received: {entry.Value}  |  Broadcast Rpcs Sent:{m_GlobalCounter} -- Direct Rpcs Sent:{(UnitTesting ? m_ServerDirectTotalRpcCount : m_GlobalDirectCounter)}\n";
                    m_ServerUpdateInfo += $"{nameof(m_MultiParameterValuesCount)} : {m_MultiParameterValuesCount}  |  {nameof(m_MultiParameterNoneCount)} : {m_MultiParameterNoneCount}\n";
                    m_ServerUpdateInfo += $"{nameof(m_RpcPerSecond)} : {m_RpcPerSecond}\n ";
                }
                else
                {
                    m_ServerUpdateInfo += $"Client-ID [{entry.Key}]  Client to Server Rpcs Received: {entry.Value}\n";
                }
            }

            m_ServerUpdateInfo += $"{nameof(m_ClientRpcDirectTestingMode)} : {m_ClientRpcDirectTestingMode}";

            if (!UnitTesting)
            {
                if (IsHost)
                {
                    if (m_GlobalDirectCurrentClientIdIndex < m_ClientIds.Count)
                    {
                        if (m_ClientProgressBar && m_ClientIndices.Contains(NetworkManager.LocalClientId))
                        {
                            m_ClientProgressBar.fillAmount = Mathf.Clamp((2.0f * m_GlobalDirectCounter) * k_ProgressBarDivisor, 0.01f, 1.0f);
                        }
                    }
                }
                m_CounterTextObject.text = m_ServerUpdateInfo;
            }
        }
    }
}
