using Unity.Netcode;
using UnityEngine;

public class ManualPlayerSpawnExtension : BaseObjectSpawnExtension
{
    protected override void OnInitialize()
    {
        PrefabToSpawn.GetComponent<MoverScriptNoRigidbody>().ManualSpawn = true;
        base.OnInitialize();
    }

    protected override bool SpawnAsPlayer()
    {
        return true;
    }

    protected override bool CanDespawnObject()
    {
        return NetworkManager.LocalClient.PlayerObject != null;
    }

    protected override bool CanSpawnObject()
    {
        return NetworkManager.LocalClient.PlayerObject == null;
    }

    protected override void DespawnObject(ulong playerId)
    {
        if (m_ExtendedNetworkManager.DistributedAuthorityMode)
        {
            base.DespawnObject(playerId);
            return;
        }
        else
        {
            var networkObject = m_ExtendedNetworkManager.SpawnManager.GetPlayerNetworkObject(playerId);
            networkObject.Despawn();
        }
    }

    protected override NetworkObject GetSpawnedNetworkObject()
    {
        if (NetworkManager.LocalClient.PlayerObject)
        {
            return NetworkManager.LocalClient.PlayerObject;
        }
        return null;
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
