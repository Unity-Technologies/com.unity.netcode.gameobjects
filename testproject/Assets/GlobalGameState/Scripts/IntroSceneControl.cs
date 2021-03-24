using UnityEngine;
using MLAPI;

public class IntroSceneControl : MonoBehaviour
{
    private void Start()
    {
#if (UNITY_EDITOR)
        if (!NetworkManager.Singleton)
        {

            GlobalGameState.LoadBootStrapScene();
        }
#endif
    }

    /// <summary>
    /// Tied to the button that transitions from intro into the main menu
    /// </summary>
    public void OnProceed()
    {
        GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.Menu);
    }

}
