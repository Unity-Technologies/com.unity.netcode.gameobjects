using System;
using Unity.Netcode;
using UnityEngine;

public class MultipleNMSceneHandlerConfig : MonoBehaviour
{
    [SerializeField]
    private NetworkManager m_NetworkManager;

    // Start is called before the first frame update
    private void Start()
    {
        if (m_NetworkManager == null)
        {
            throw new NotSupportedException();
        }
        m_NetworkManager.OnInitializedEvent += (sender, args) => (sender as NetworkManager).SceneManager.SceneManagerHandler = new MultiSceneManagerHandler();
    }
}
