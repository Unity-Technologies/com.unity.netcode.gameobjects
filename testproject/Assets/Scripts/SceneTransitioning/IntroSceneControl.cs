using UnityEngine;

public class IntroSceneControl : MonoBehaviour
{

    public void OnProceed()
    {
        GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.Menu);
    }

}
