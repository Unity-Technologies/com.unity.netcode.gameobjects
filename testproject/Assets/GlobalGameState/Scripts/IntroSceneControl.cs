using UnityEngine;
using MLAPI;

public class IntroSceneControl : MonoBehaviour
{
    private void Start()
    {
        NetworkManager NM = NetworkManager.Singleton;
        if (NM == null)
        {
#if (UNITY_EDITOR)
            GlobalGameState.LoadBootStrapScene();
#endif
        }
    }

    /// <summary>
    /// Tied to the button that transitions from intro into the main menu
    /// </summary>
    public void OnProceed()
    {
        GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.Menu);
    }

}
