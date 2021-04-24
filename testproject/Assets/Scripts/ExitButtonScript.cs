using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;

public class ExitButtonScript : MonoBehaviour
{
    [SerializeField]
    private SceneMenuReference m_SceneMenuToLoad;

    /// <summary>
    /// A very basic way to exit back to the first scene in the build settings
    /// </summary>
    public void OnExitScene()
    {
        if (NetworkManager.Singleton)
        {
            if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.StopClient();
            }
            else if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.StopHost();
            }
            else if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.StopServer();
            }
            Destroy(NetworkManager.Singleton);
        }

        if (m_SceneMenuToLoad != null && m_SceneMenuToLoad.SceneName != string.Empty)
        {           
            SceneManager.LoadSceneAsync(m_SceneMenuToLoad.SceneName, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
        }
        
    }
}
