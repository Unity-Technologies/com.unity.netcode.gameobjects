using Unity.Netcode;
using UnityEngine;

public class BaseObjectSpawnExtension : BaseNetcodeExtension
{
    public GameObject PrefabToSpawn;
    [Range(2.0f, 20.0f)]
    public float SpawnRadius = 5.0f;
    public bool DestroyWithScene = false;

    private Vector3 m_InitialPrefabPosition = Vector3.zero;

    private GameObject m_AuthorityGameObjectInstance;
    private NetworkObject m_AuthorityNetworkObjectInstance;
    protected MoverScriptNoRigidbody m_MoverScriptNoRigidbody { get; private set; }

    protected override void OnInitialize()
    {
        m_InitialPrefabPosition = PrefabToSpawn.transform.position;
        var networkObject = PrefabToSpawn.GetComponent<NetworkObject>();
        networkObject.DestroyWithScene = DestroyWithScene;
        MoverScriptNoRigidbody.SetCameraTransform(Camera.main.transform);
        base.OnInitialize();
    }

    protected override void OnNonAuthorityUpdate()
    {
        GeneralUpdate();
        base.OnNonAuthorityUpdate();
    }

    protected override void OnAuthorityUpdate()
    {
        GeneralUpdate();
        base.OnAuthorityUpdate();
    }

    protected virtual GameObject GetSpawnedObject()
    {
        return m_AuthorityGameObjectInstance;
    }

    protected virtual NetworkObject GetSpawnedNetworkObject()
    {
        return m_AuthorityNetworkObjectInstance;
    }

    protected virtual bool CanSpawnObject()
    {
        return GetSpawnedObject() == null;
    }

    protected virtual bool CanDespawnObject()
    {
        return GetSpawnedObject() != null;
    }

    protected virtual bool SpawnAsPlayer()
    {
        return false;
    }

    protected virtual void OnObjectDespawned()
    {

    }

    protected virtual void OnGeneralUpdate()
    {

    }

    private void GeneralUpdate()
    {
        if (!IsSpawned)
        {
            return;
        }
        OnGeneralUpdate();
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (CanSpawnObject())
            {
                if (!m_ExtendedNetworkManager.DistributedAuthorityMode)
                {
                    SpawnObjectRpc(SpawnAsPlayer());
                }
                else
                {
                    SpawnObject(m_ExtendedNetworkManager.LocalClientId, SpawnAsPlayer());
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (CanDespawnObject())
            {
                if (!m_ExtendedNetworkManager.DistributedAuthorityMode)
                {
                    DespawnObjectRpc();
                }
                else
                {
                    DespawnObject();
                }
            }
        }
    }

    protected virtual void SpawnObject(ulong playerId, bool isPlayerObject)
    {
        var randomPosition = new Vector3(Random.Range(-SpawnRadius, SpawnRadius), 1.25f, Random.Range(-SpawnRadius, SpawnRadius));
        m_AuthorityNetworkObjectInstance = NetworkObject.InstantiateAndSpawn(PrefabToSpawn, NetworkManager, playerId, false, isPlayerObject, position: m_InitialPrefabPosition + randomPosition);
        m_AuthorityGameObjectInstance = m_AuthorityNetworkObjectInstance.gameObject;
    }

    [Rpc(SendTo.Server)]
    private void SpawnObjectRpc(bool isPlayerObject, RpcParams rpcParams = default)
    {
        SpawnObject(rpcParams.Receive.SenderClientId, isPlayerObject);
    }

    protected virtual void DespawnObject()
    {
        if (CanDespawnObject() && m_AuthorityNetworkObjectInstance.HasAuthority)
        {
            m_AuthorityNetworkObjectInstance.Despawn();
            m_AuthorityNetworkObjectInstance = null;
            m_AuthorityGameObjectInstance = null;
            OnObjectDespawned();
        }
    }

    [Rpc(SendTo.Authority)]
    private void DespawnObjectRpc(RpcParams rpcParams = default)
    {
        if (m_AuthorityNetworkObjectInstance)
        {
            DespawnObject();
        }
    }

    protected override Rect OnGUIUpdate(Rect totalRectSize, ScreenSpaceRegions screenSpaceRegion)
    {
        if (!IsSpawned)
        {
            return totalRectSize;
        }
        switch (screenSpaceRegion)
        {
            case ScreenSpaceRegions.TopRight:
                {
                    if (m_ConnectionState == ConnectionStates.Connected)
                    {
                        totalRectSize = DrawLabel(totalRectSize, $"[Enter] Spawn Player");
                        totalRectSize = DrawLabel(totalRectSize, $"[Backspace] Despawn Player");
                    }
                    break;
                }
        }
        return base.OnGUIUpdate(totalRectSize, screenSpaceRegion);
    }
}
