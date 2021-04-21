using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;

public class ExitButtonScript : MonoBehaviour
{
    /// <summary>
    /// A very basic way to exit back to the first scene in the build settings
    /// </summary>
    public void OnExitScene()
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

        Destroy(NetworkManager.Singleton.gameObject);
        SceneManager.LoadSceneAsync(0,LoadSceneMode.Single);
    }

}
