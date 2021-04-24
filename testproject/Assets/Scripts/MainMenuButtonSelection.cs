using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButtonSelection : MonoBehaviour
{
    public string SceneToLoad;

    public void OnLoadScene()
    {
        if(SceneToLoad != string.Empty)
        {
            SceneManager.LoadScene(SceneToLoad);
        }
    }
}
