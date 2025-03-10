using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoadingExtension : BaseMonoExtension
{
#if UNITY_EDITOR
    public List<SceneAsset> Scenes = new List<SceneAsset>();
    public SceneAsset HUDScene;
    public SceneAsset MainMenuScene;

    protected override void OnValidateComponent()
    {
        m_MainMenuSceneName = string.Empty;
        if (MainMenuScene)
        {
            m_MainMenuSceneName = MainMenuScene.name;
        }
        if (HUDScene != null)
        {
            m_HUDSceneName = HUDScene.name;
        }
        m_SceneNames.Clear();
        if (Scenes.Count > 0)
        {
            foreach (var scene in Scenes)
            {
                if (!scene)
                {
                    continue;
                }
                m_SceneNames.Add(scene.name);
            }
        }
        base.OnValidateComponent();
    }
#endif
    public KeyCode NextSceneKeyCode = KeyCode.Tab;

    [HideInInspector]
    [SerializeField]
    private string m_MainMenuSceneName;

    [HideInInspector]
    [SerializeField]
    private string m_HUDSceneName;

    [HideInInspector]
    [SerializeField]
    private List<string> m_SceneNames = new List<string>();
    private string m_CurrentSceneName;

    private bool m_SceneEventIsLoading;
    private bool m_HasDisconnectToSceneName;

    private void LoadNextScene()
    {
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var sceneNameIndex = m_SceneNames.IndexOf(activeScene.name);
        sceneNameIndex = (sceneNameIndex + 1) % m_SceneNames.Count;
        m_CurrentSceneName = m_SceneNames[sceneNameIndex];
        var result = SceneManager.LoadScene(m_CurrentSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        if (result == SceneEventProgressStatus.Started)
        {
            m_SceneEventIsLoading = true;
            SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }
    }

    private void OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        if (string.IsNullOrEmpty(m_HUDSceneName))
        {
            m_SceneEventIsLoading = false;
        }
        else
        {
            var result = SceneManager.LoadScene(m_HUDSceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            if (result == SceneEventProgressStatus.Started)
            {
                SceneManager.OnLoadEventCompleted += OnLoadHUDEventCompleted;
            }
        }
    }

    private void OnLoadHUDEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        SceneManager.OnLoadEventCompleted -= OnLoadHUDEventCompleted;
        m_SceneEventIsLoading = false;

    }

    protected override void OnInitialize()
    {
        m_HasDisconnectToSceneName = !string.IsNullOrEmpty(m_MainMenuSceneName);
        if (m_SceneNames.Count > 0)
        {
            m_CurrentSceneName = m_SceneNames[0];
            UnityEngine.SceneManagement.SceneManager.LoadScene(m_SceneNames[0]);
            if (!string.IsNullOrEmpty(m_HUDSceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(m_HUDSceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }
        }
        base.OnInitialize();
    }

    protected override void OnStatusUpdate(ConnectionStates previousState, ConnectionStates currentState)
    {
        if (previousState == ConnectionStates.Connected && currentState == ConnectionStates.None)
        {
            if (m_CurrentSceneName != m_SceneNames[0])
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(m_SceneNames[0]);
                if (!string.IsNullOrEmpty(m_HUDSceneName))
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(m_HUDSceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                }
            }
        }
        else if (currentState == ConnectionStates.Connected)
        {
            if (IsAuthorityInstance())
            {
                m_ExtendedNetworkManager.SceneManager.SetClientSynchronizationMode(UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }
        }
        base.OnStatusUpdate(previousState, currentState);
    }

    protected override void OnAuthorityUpdate()
    {
        if (!m_SceneEventIsLoading && m_SceneNames.Count > 1 && Input.GetKeyDown(NextSceneKeyCode))
        {
            LoadNextScene();
        }
        base.OnAuthorityUpdate();
    }

    private Rect ReturnToMainMenu(Rect totalRectSize)
    {
        var retButtonValues = Draw.Button(totalRectSize, "Main Menu");
        if (retButtonValues.Item2)
        {
            totalRectSize = retButtonValues.Item1;
            Destroy(Camera.main);
            if (m_ConnectionState == ConnectionStates.Connected || m_ExtendedNetworkManager.IsListening)
            {
                m_ExtendedNetworkManager.OnClientStopped += OnStopped;
                m_ExtendedNetworkManager.OnServerStopped += OnStopped;
                m_ExtendedNetworkManager.Shutdown();
            }
            else
            {
                ReturnMainMenu();
            }
        }
        return totalRectSize;
    }

    private void ReturnMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += ExitSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(m_MainMenuSceneName);
    }

    private void ExitSceneLoaded(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.LoadSceneMode arg1)
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= ExitSceneLoaded;
        if (m_ExtendedNetworkManager.gameObject)
        {
            Destroy(m_ExtendedNetworkManager.gameObject);
        }
    }

    private void OnStopped(bool wasHost)
    {
        m_ExtendedNetworkManager.OnClientStopped -= OnStopped;
        m_ExtendedNetworkManager.OnServerStopped -= OnStopped;
        ReturnMainMenu();
    }

    protected override Rect OnGUIUpdate(Rect totalRectSize, ScreenSpaceRegions screenSpaceRegion)
    {
        switch (screenSpaceRegion)
        {
            case ScreenSpaceRegions.TopLeft:
                {
                    if (m_ConnectionState == ConnectionStates.Connected && IsAuthorityInstance())
                    {
                        totalRectSize = Draw.Label(totalRectSize, $"Current Scene: {m_CurrentSceneName}");
                    }
                    break;
                }
            case ScreenSpaceRegions.TopRight:
                {
                    if (m_HasDisconnectToSceneName)
                    {
                        totalRectSize = ReturnToMainMenu(totalRectSize);
                    }
                    if (IsAuthorityInstance() && m_ConnectionState == ConnectionStates.Connected)
                    {
                        // If there is only one scene then no need to draw this
                        if (m_SceneNames.Count > 1)
                        {
                            totalRectSize = Draw.Label(totalRectSize, $"[{NextSceneKeyCode}] Load Next Scene");
                        }
                    }
                    break;
                }
        }
        return base.OnGUIUpdate(totalRectSize, screenSpaceRegion);
    }
}
