using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;

public class ExitButtonScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }


    public void OnExitScene()
    {
        if(NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.StopClient();
        }
        else if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.StopHost();
        }

        Destroy(NetworkManager.Singleton.gameObject);
        SceneManager.LoadSceneAsync(0,LoadSceneMode.Single);
    }

}
