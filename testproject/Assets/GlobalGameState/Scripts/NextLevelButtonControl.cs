using UnityEngine;
using UnityEngine.UI;
using MLAPI;

public class NextLevelButtonControl : NetworkBehaviour
{
    [SerializeField]
    Button m_NextLevelButton;

    private void Start()
    {
        if (!IsServer && m_NextLevelButton)
        {
            m_NextLevelButton.gameObject.SetActive(false);
        }
    }
}
