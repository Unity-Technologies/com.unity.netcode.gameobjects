using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class ExitButtonScript : MonoBehaviour
{
    [SerializeField]
    private MenuReference m_SceneMenuToLoad;

    /// <summary>
    /// A very basic way to exit back to the first scene in the build settings
    /// </summary>
    public void OnExitScene()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }

        if (m_SceneMenuToLoad != null && m_SceneMenuToLoad.GetReferencedScenes()[0] != string.Empty)
        {
            SceneManager.LoadSceneAsync(m_SceneMenuToLoad.GetReferencedScenes()[0], LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
        }

    }
}
