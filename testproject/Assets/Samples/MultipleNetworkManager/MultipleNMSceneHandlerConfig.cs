using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MultipleNMSceneHandlerConfig : MonoBehaviour
{
    public NetworkManager m_NetworkManager;
    // Start is called before the first frame update
    void Start()
    {
        if (m_NetworkManager == null)
        {
            throw new NotSupportedException();
        }
        m_NetworkManager.OnInitializedEvent += (sender, args) => (sender as NetworkManager).SceneManager.SceneManagerHandler = new MultiSceneManagerHandler();
    }
}
