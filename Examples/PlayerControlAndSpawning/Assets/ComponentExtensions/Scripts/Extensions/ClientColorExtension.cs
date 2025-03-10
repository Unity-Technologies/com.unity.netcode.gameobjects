using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ClientColorExtension : BaseNetcodeExtension
{
    private static Color[] s_Colors = { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow };

    public static ClientColorExtension Instance;

    public static Color GetClientColor(ulong clientId)
    {
        if (Instance)
        {
            return Instance.FindClientColor(clientId);
        }
        return Color.gray;
    }

    public Action ClientColorsChanged;

    private Dictionary<ulong, Color> m_ClientColors = new Dictionary<ulong, Color>();

    public Color LocalClientColor { get; private set; }
    private Identifier m_Identifier;

    protected override void OnInitialize()
    {
        m_Identifier = GetComponent<Identifier>();
        m_ExtendedNetworkManager.OnConnectionEvent += ConnectionEvent;
        if (Instance && Instance.gameObject)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
        base.OnInitialize();
    }

    private Color FindClientColor(ulong clientId)
    {
        if (m_ClientColors.ContainsKey(clientId))
        {
            return m_ClientColors[clientId];
        }
        else
        {
            return UpdateClientColor(clientId);
        }
    }

    private void ConnectionEvent(NetworkManager arg1, ConnectionEventData eventData)
    {
        switch (eventData.EventType)
        {
            case Unity.Netcode.ConnectionEvent.PeerConnected:
                {
                    UpdateClientColor(eventData.ClientId);
                    break;
                }
            case Unity.Netcode.ConnectionEvent.PeerDisconnected:
                {
                    if (m_ClientColors.ContainsKey(eventData.ClientId))
                    {
                        m_ClientColors.Remove(eventData.ClientId);
                    }
                    break;
                }
            case Unity.Netcode.ConnectionEvent.ClientConnected:
                {
                    if (eventData.ClientId == m_ExtendedNetworkManager.LocalClientId)
                    {
                        LocalClientColor = AssignClientColor(m_ExtendedNetworkManager.LocalClientId);
                        UpdateClientColor(m_ExtendedNetworkManager.LocalClientId);
                    }
                    break;
                }
            case Unity.Netcode.ConnectionEvent.ClientDisconnected:
                {
                    if (eventData.ClientId == m_ExtendedNetworkManager.LocalClientId)
                    {
                        if (m_ClientColors.ContainsKey(eventData.ClientId))
                        {
                            m_ClientColors.Remove(eventData.ClientId);
                        }
                        LocalClientColor = Color.gray;
                        m_Identifier.SetColor(LocalClientColor);
                    }
                    break;
                }
        }
    }

    private Color UpdateClientColor(ulong clientId)
    {
        var clientColor = AssignClientColor(clientId);
        if (!m_ClientColors.ContainsKey(clientId))
        {
            m_ClientColors.Add(clientId, clientColor);
        }
        else
        {
            m_ClientColors[clientId] = clientColor;
        }
        return clientColor;
    }

    private Color AssignClientColor(ulong clientId)
    {
        ulong myId = clientId - (ulong)(m_ExtendedNetworkManager.DistributedAuthorityMode && m_ExtendedNetworkManager.CMBServiceConnection ? 1 : 0);
        return s_Colors[myId % Convert.ToUInt64(s_Colors.Length)];
    }

    protected override void OnNetworkPostSpawn()
    {
        UpdateLocalClientColor();
        base.OnNetworkPostSpawn();
    }

    private void UpdateLocalClientColor()
    {
        LocalClientColor = AssignClientColor(m_ExtendedNetworkManager.LocalClientId);
        m_Identifier.SetColor(LocalClientColor);
    }
}
