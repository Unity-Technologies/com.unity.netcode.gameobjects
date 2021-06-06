using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.SceneManagement;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class TestSceneSwitcher : NetworkBehaviour
{
    [SerializeField]
    private BoolScriptableObject shouldSwitchScene;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (shouldSwitchScene.value)
        {
            SwitchSceneServerRpc();
            shouldSwitchScene.value = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SwitchSceneServerRpc()
    {
        NetworkManager.Singleton.SceneManager.SwitchScene("TargetScene");
    }
}
