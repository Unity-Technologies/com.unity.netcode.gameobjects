using Unity.Netcode;
using UnityEngine;

public class ManualPlayerSpawnExtension : BaseObjectSpawnExtension
{
    protected override void OnInitialize()
    {
        PrefabToSpawn.GetComponent<MoverScriptNoRigidbody>().ManualSpawn = true;
        base.OnInitialize();
    }

    protected override GameObject GetSpawnedObject()
    {
        if (NetworkManager.LocalClient.PlayerObject)
        {
            return NetworkManager.LocalClient.PlayerObject.gameObject;
        }
        return null;
    }
}
