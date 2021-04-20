using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Profiling;
using UnityEngine;
using UnityEngine.UI;

public class StatsDisplay : NetworkBehaviour
{
    [SerializeField]
    private GameObject m_ClientServerToggle;
    private bool m_ClientMode = true;
    private Rect m_Stats;
    private string m_LastStatsDump;
    private Text m_ClientServerToggleText;
    private List<ulong> m_ClientsToUpdate = new List<ulong>();


    private bool m_IsServer;

    private void Start()
    {
        m_Stats = new Rect(5, 10, 175, 300);
        GUI.contentColor = new Color(196, 196, 196, 196);
        GUI.backgroundColor = new Color(96, 96, 96, 96);
        if (m_ClientServerToggle != null)
        {
            m_ClientServerToggleText = m_ClientServerToggle.GetComponentInChildren<Text>();
        }

        if(NetworkManager && NetworkManager.IsListening && !NetworkManager.IsServer)
        {
            m_ClientServerToggle.SetActive(true);
        }
        else
        {
            m_ClientServerToggle.SetActive(false);
        }
  
    }

    public override void NetworkStart()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            m_IsServer = true;
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        }
        else
        {
            m_ClientServerToggle.SetActive(true);
            UpdateButton();
        }
        StartCoroutine("UpdateTextStatus");
    }

    private void Singleton_OnClientConnectedCallback(ulong clientId)
    {
        if (!IsServer)
        {
            var networkObject = GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.SpawnWithOwnership(clientId, null, true);
            }
        }
    }

    public void ToggleClientSever()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            m_ClientMode = !m_ClientMode;
            GetStatsServerRPC(NetworkManager.Singleton.LocalClientId);
            UpdateButton();
        }
    }
    private void OnGUI()
    {
        if (NetworkManager && NetworkManager.IsListening)
        {
            GUI.TextArea(m_Stats, m_LastStatsDump);
        }
    }

    private void UpdateButton()
    {
        if (m_ClientServerToggleText != null)
        {
            m_ClientServerToggleText.text = m_ClientMode ? "Show Server Stats" : "Show Client Stats";
        }
    }
 
    [ClientRpc]
    private void ReceiveStatsClientRPC(StatsInfoContainer statsinfo)
    {
        m_LastStatsDump = "Server Stats";
        m_LastStatsDump += "\ndeltaTime: [" + Time.deltaTime.ToString() + "]";
        if (ProfilerStatManager.AllStats.Count != statsinfo.StatValues.Count)
        {
            Debug.LogError("[StatsDisplay-Error][Mismatch] Recieved " + statsinfo.StatValues.Count.ToString() + " values and have " + ProfilerStatManager.AllStats.Count.ToString() + " profiler stats entries!");
        }
        else
        {
            var statsCounter = 0;
            foreach (ProfilerStat p in ProfilerStatManager.AllStats)
            {
                if (m_LastStatsDump != string.Empty)
                {
                    m_LastStatsDump += "\n";
                }
                m_LastStatsDump += p.PrettyPrintName + ": " + statsinfo.StatValues[statsCounter].ToString(("0.0"));
                statsCounter++;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetStatsServerRPC(ulong clientId)
    {
        if (!m_ClientsToUpdate.Contains(clientId))
        {
            m_ClientsToUpdate.Add(clientId);
        }
        else if (m_ClientsToUpdate.Contains(clientId))
        {
            m_ClientsToUpdate.Remove(clientId);
        }
    }
    private IEnumerator UpdateTextStatus()
    {
        while (true)
        {
            if (m_ClientMode)
            {
                m_LastStatsDump = m_IsServer ? "Server Stats":"Client Stats";
                m_LastStatsDump += "\ndeltaTime: [" + Time.deltaTime.ToString() + "]";
                foreach (ProfilerStat p in ProfilerStatManager.AllStats)
                {
                    if (m_LastStatsDump != string.Empty)
                    {
                        m_LastStatsDump += "\n";
                    }
                    m_LastStatsDump += p.PrettyPrintName + ": " + p.SampleRate().ToString("0.0");
                }
            }
            if (NetworkManager.Singleton.IsServer && m_ClientsToUpdate.Count > 0)
            {
                var statsInfoContainer = new StatsInfoContainer();
                statsInfoContainer.StatValues = new List<float>();
                foreach (ProfilerStat p in ProfilerStatManager.AllStats)
                {
                    statsInfoContainer.StatValues.Add(p.SampleRate());
                }
                ReceiveStatsClientRPC(statsInfoContainer);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}

