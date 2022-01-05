using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class SimpleSpawn : NetworkBehaviour, INetworkPrefabInstanceHandler
{
    [SerializeField]
    private GameObject m_ObjectToSpawn;

    private GameObject m_ObjectInstance;

    [SerializeField]
    private Button m_SpawnObjectButton;

    private NetworkObject m_NetworkObjectSpawned;

    public override void OnNetworkSpawn()
    {
        if (NetworkManager && NetworkManager.PrefabHandler != null)
        {
            NetworkManager.PrefabHandler.AddHandler(m_ObjectToSpawn, this);
        }

        if (m_ObjectInstance == null)
        {
            m_ObjectInstance = Instantiate(m_ObjectToSpawn);
            m_ObjectInstance.SetActive(false);
        }

        if (!IsServer)
        {
            gameObject.SetActive(false);
        }
        else
        {
            m_SpawnObjectButton?.gameObject.SetActive(true);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager && NetworkManager.PrefabHandler != null)
        {
            NetworkManager.PrefabHandler.RemoveHandler(m_ObjectToSpawn);
        }
    }

    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        m_ObjectInstance.SetActive(true);
        m_ObjectInstance.transform.position = position;
        m_ObjectInstance.transform.rotation = rotation;
        return m_ObjectInstance.GetComponent<NetworkObject>();
    }

    public void Destroy(NetworkObject networkObject)
    {
        networkObject.gameObject.SetActive(false);
    }

    public void SpawnObject()
    {
        if (IsServer && IsSpawned && m_ObjectToSpawn != null)
        {
            if (m_NetworkObjectSpawned == null)
            {
                m_NetworkObjectSpawned = m_ObjectInstance.GetComponent<NetworkObject>();
            }

            if (m_NetworkObjectSpawned != null && !m_NetworkObjectSpawned.IsSpawned)
            {
                m_ObjectInstance.SetActive(true);
                m_ObjectInstance.transform.position = transform.position;
                m_NetworkObjectSpawned.Spawn(true);
            }
        }
    }
}
