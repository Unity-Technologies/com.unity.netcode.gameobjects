using System.Text;
using Unity.Netcode;
using UnityEngine;

public class SessionInfoExtension : BaseMonoExtension
{
    private StringBuilder m_ScenesPreloaded = new StringBuilder();

    public string SessionName { get; private set; }

    protected override void OnStatusUpdate(ConnectionStates previousState, ConnectionStates currentState)
    {
        if (previousState == ConnectionStates.Connected && currentState == ConnectionStates.None)
        {
            m_ScenesPreloaded.Clear();
        }
        if (previousState == ConnectionStates.Connecting && currentState == ConnectionStates.Connected)
        {
            m_ExtendedNetworkManager.LogMessage($"Connected to session {SessionName}.");
        }
        base.OnStatusUpdate(previousState, currentState);
    }

    private Rect OnDrawLiveServiceGUI(Rect currentRect)
    {
        var retFieldValues = Draw.TextField(currentRect, SessionName);
        currentRect = retFieldValues.Item1;
        SessionName = retFieldValues.Item2;

        var retButtonValues = Draw.Button(currentRect, "Create or Connect To Session");

        if (retButtonValues.Item2)
        {
            currentRect = retButtonValues.Item1;
            m_ExtendedNetworkManager.LogMessage($"Connecting to session {SessionName}...");
            m_ExtendedNetworkManager.CreateOrConnectToSession(SessionName);
        }

        return currentRect;
    }

    private Rect OnDrawHostedGUI(Rect currentRect)
    {
        var prefixText = m_ExtendedNetworkManager.NetworkConfig.NetworkTopology == NetworkTopologyTypes.DistributedAuthority ? "DA-" : "";
        var retValues = Draw.Button(currentRect, $"Start {prefixText}Host");
        if (retValues.Item2)
        {
            currentRect = retValues.Item1;
            m_ExtendedNetworkManager.StartClientHostedSession(true);
        }

        retValues = Draw.Button(currentRect, $"Start {prefixText}Client");
        if (retValues.Item2)
        {
            currentRect = retValues.Item1;
            m_ExtendedNetworkManager.StartClientHostedSession(false);
        }
        return currentRect;
    }


    private Rect OnUpdateGUIDisconnected(Rect currentRect)
    {
        var connectionType = m_ExtendedNetworkManager.ConnectionType;
        if (m_ExtendedNetworkManager.NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer && connectionType != ExtendedNetworkManager.ConnectionTypes.Host)
        {
            connectionType = ExtendedNetworkManager.ConnectionTypes.Host;
        }

        switch (connectionType)
        {
            case ExtendedNetworkManager.ConnectionTypes.LiveService:
                {
                    currentRect = OnDrawLiveServiceGUI(currentRect);
                    break;
                }
            case ExtendedNetworkManager.ConnectionTypes.Host:
                {
                    currentRect = OnDrawHostedGUI(currentRect);
                    break;
                }
        }

        if (m_ScenesPreloaded.Length == 0)
        {
            m_ScenesPreloaded.Append("Scenes Preloaded: ");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                m_ScenesPreloaded.Append($"[{scene.name}]");
            }
        }

        currentRect = Draw.Label(currentRect, m_ScenesPreloaded.ToString());
        return currentRect;
    }

    private Rect OnUpdateGUIConnected(Rect currentRect)
    {
        if (m_ExtendedNetworkManager.CMBServiceConnection)
        {
            currentRect = Draw.Label(currentRect, $"Session: {SessionName}");
        }
        else
        {
            if (m_ExtendedNetworkManager.DistributedAuthorityMode)
            {
                currentRect = Draw.Label(currentRect, $"DAHosted Session");
            }
            else
            {
                currentRect = Draw.Label(currentRect, $"Client-Server Session");
            }
        }
        return currentRect;
    }

    private Rect TopLeftGUI(Rect totalRectSize)
    {
        switch (m_ConnectionState)
        {
            case ConnectionStates.None:
                {
                    totalRectSize = OnUpdateGUIDisconnected(totalRectSize);
                    break;
                }
            case ConnectionStates.Connected:
                {
                    totalRectSize = OnUpdateGUIConnected(totalRectSize);
                    break;
                }
        }
        return totalRectSize;
    }

    private Rect TopRightGUI(Rect totalRectSize)
    {
        if (m_ConnectionState == ConnectionStates.Connected)
        {
            var retButtonValues = Draw.Button(totalRectSize, "Disconnect");
            if (retButtonValues.Item2)
            {
                totalRectSize = retButtonValues.Item1;
                m_ExtendedNetworkManager.DisconnectFromSession();
            }
        }
        return totalRectSize;
    }

    protected override Rect OnGUIUpdate(Rect totalRectSize, ScreenSpaceRegions screenSpaceRegion)
    {
        switch (screenSpaceRegion)
        {
            case ScreenSpaceRegions.TopLeft:
                {
                    totalRectSize = TopLeftGUI(totalRectSize);
                    break;
                }
            case ScreenSpaceRegions.TopRight:
                {
                    totalRectSize = TopRightGUI(totalRectSize);
                    break;
                }
        }
        return base.OnGUIUpdate(totalRectSize, screenSpaceRegion);
    }
}
