using System.Text;
using Unity.Netcode;
using UnityEngine;

public class SessionInfoExtension : BaseMonoExtension
{
    private StringBuilder m_ScenesPreloaded = new StringBuilder();

    public string SessionName { get; private set; }

    protected override void OnInitialize()
    {
        if (Camera.main)
        {
            MoverScriptNoRigidbody.SetCameraTransform(Camera.main.transform);
        }
        base.OnInitialize();
    }

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
        var retFieldValues = DrawTextField(currentRect, SessionName);
        currentRect = retFieldValues.Item1;
        SessionName = retFieldValues.Item2;

        var retButtonValues = DrawButton(currentRect, "Create or Connect To Session");

        if (retButtonValues.Item2)
        {
            currentRect = retButtonValues.Item1;
            m_ExtendedNetworkManager.LogMessage($"Connecting to session {SessionName}...");
            m_ExtendedNetworkManager.CreateOrConnectToSession(SessionName);
        }

        return currentRect;
    }

    private Rect OnDrawDAHostGUI(Rect currentRect)
    {
        var retValues = DrawButton(currentRect, "Start Host");
        if (retValues.Item2)
        {
            currentRect = retValues.Item1;
            m_ExtendedNetworkManager.StartClientHostedSession(true);
        }

        retValues = DrawButton(currentRect, "Start Client");
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
                    currentRect = OnDrawDAHostGUI(currentRect);
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

        currentRect = DrawLabel(currentRect, m_ScenesPreloaded.ToString());
        return currentRect;
    }

    private Rect OnUpdateGUIConnected(Rect currentRect)
    {
        if (m_ExtendedNetworkManager.CMBServiceConnection)
        {
            currentRect = DrawLabel(currentRect, $"Session: {SessionName}");
        }
        else
        {
            if (m_ExtendedNetworkManager.DistributedAuthorityMode)
            {
                currentRect = DrawLabel(currentRect, $"DAHosted Session");
            }
            else
            {
                currentRect = DrawLabel(currentRect, $"Client-Server Session");
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
            var retButtonValues = DrawButton(totalRectSize, "Disconnect");
            if (retButtonValues.Item2)
            {
                totalRectSize = retButtonValues.Item1;
                MoverScriptNoRigidbody.ResetCamera();
                m_ExtendedNetworkManager.DisconnectFromSession();
            }
        }
        return totalRectSize;
    }

    protected override Rect OnGUIUpdate(Rect totalRectSize, ScreenSpaceRegions screenSpaceRegion)
    {
        if (m_ApplicationExitPending)
        {
            return totalRectSize;
        }

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
