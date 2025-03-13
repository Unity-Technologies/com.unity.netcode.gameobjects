using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;

[Serializable]
public class SceneAssetEntry
{
    [Tooltip("This scene will be loaded in LoadSceneMode.Single")]
    public SceneAsset ActiveScene;
    [Tooltip("Scenes loaded additively once the ActiveScene is loaded.")]
    public List<SceneAsset> AdditveScenes;
}
#endif

public class SceneLoadingExtension : BaseMonoExtension
{
    [Serializable]
    internal struct SceneEntry : IEquatable<SceneEntry>
    {
        public int Index;
        public string ActiveScene;
        public List<string> AdditveScenes;

        public bool Equals(SceneEntry other)
        {
            return Index != other.Index;
        }
    }
#if UNITY_EDITOR
    [Tooltip("The scene to return back to when exiting the ActiveScene.")]
    public SceneAsset MainMenuScene;

    public List<SceneAssetEntry> Scenes = new List<SceneAssetEntry>();

    protected override void OnValidateComponent()
    {
        m_MainMenuSceneName = string.Empty;
        if (MainMenuScene)
        {
            m_MainMenuSceneName = MainMenuScene.name;
        }
        m_SceneEntries.Clear();
        var indexCount = 0;
        foreach (var sceneAssetEntry in Scenes)
        {
            if (!sceneAssetEntry.ActiveScene)
            {
                continue;
            }
            var sceneEntry = new SceneEntry()
            {
                Index = indexCount,
                ActiveScene = sceneAssetEntry.ActiveScene.name,
                AdditveScenes = new List<string>(),

            };
            indexCount++;
            foreach (var additiveScene in sceneAssetEntry.AdditveScenes)
            {
                if (additiveScene)
                {
                    sceneEntry.AdditveScenes.Add(additiveScene.name);
                }
            }
            m_SceneEntries.Add(sceneEntry);
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
    private List<SceneEntry> m_SceneEntries = new List<SceneEntry>();
    private SceneEntry m_CurrentSceneEntry;

    private bool m_SceneEventIsLoading;
    private bool m_HasDisconnectToSceneName;
    private int m_CurrentAdditiveSceneIndex;

    private void LoadNextScene()
    {
        var sceneNameIndex = (m_CurrentSceneEntry.Index + 1) % m_SceneEntries.Count;
        m_CurrentSceneEntry = m_SceneEntries[sceneNameIndex];
        var result = SceneManager.LoadScene(m_CurrentSceneEntry.ActiveScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
        if (result == SceneEventProgressStatus.Started)
        {
            m_SceneEventIsLoading = true;
            SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }
    }

    private void OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;

        if (m_CurrentSceneEntry.AdditveScenes.Count > 0)
        {
            m_CurrentAdditiveSceneIndex = 0;
            LoadNextAdditiveScene();
        }
    }

    private void LoadNextAdditiveScene()
    {
        if (m_CurrentAdditiveSceneIndex >= m_CurrentSceneEntry.AdditveScenes.Count)
        {
            SceneManager.OnLoadEventCompleted -= OnLoadAdditiveSceneCompleted;
            m_SceneEventIsLoading = false;
            return;
        }

        var result = SceneManager.LoadScene(m_CurrentSceneEntry.AdditveScenes[m_CurrentAdditiveSceneIndex], UnityEngine.SceneManagement.LoadSceneMode.Additive);
        if (result == SceneEventProgressStatus.Started)
        {
            SceneManager.OnLoadEventCompleted += OnLoadAdditiveSceneCompleted;
        }
    }


    private void OnLoadAdditiveSceneCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        m_CurrentAdditiveSceneIndex++;
        LoadNextAdditiveScene();
    }

    protected override void OnInitialize()
    {
        m_HasDisconnectToSceneName = !string.IsNullOrEmpty(m_MainMenuSceneName);
        LoadFirstSceneEntry();
        base.OnInitialize();
    }

    private void LoadFirstSceneEntry()
    {
        if (m_SceneEntries.Count > 0)
        {
            m_CurrentAdditiveSceneIndex = 0;
            m_CurrentSceneEntry = m_SceneEntries[0];
            UnityEngine.SceneManagement.SceneManager.LoadScene(m_CurrentSceneEntry.ActiveScene);
            while (m_CurrentAdditiveSceneIndex < m_CurrentSceneEntry.AdditveScenes.Count)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(m_CurrentSceneEntry.AdditveScenes[m_CurrentAdditiveSceneIndex], UnityEngine.SceneManagement.LoadSceneMode.Additive);
                m_CurrentAdditiveSceneIndex++;
            }
        }
    }

    protected override void OnStatusUpdate(ConnectionStates previousState, ConnectionStates currentState)
    {
        if (previousState == ConnectionStates.Connected && currentState == ConnectionStates.None)
        {
            LoadFirstSceneEntry();
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
        if (!m_SceneEventIsLoading && m_SceneEntries.Count > 1 && Input.GetKeyDown(NextSceneKeyCode))
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
                        totalRectSize = Draw.Label(totalRectSize, $"Current Scene: {m_CurrentSceneEntry.ActiveScene}");
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
                        if (m_SceneEntries.Count > 1)
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
