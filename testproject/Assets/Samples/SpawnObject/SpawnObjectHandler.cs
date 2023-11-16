using System.Collections.Generic;
using Unity.Netcode;
using TestProject.ManualTests;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SpawnObjectHandler : NetworkBehaviour
{
    public Button SpawnObjectButton;
    public Dropdown ObjectToSpawnDropDown;
    public List<NetworkObject> PrefabsToSpawn;

    private Dictionary<int, NetworkObject> m_OptionToNetworkObject = new Dictionary<int, NetworkObject>();


    private void Start()
    {
        SpawnObjectButton?.gameObject.SetActive(false);
        ObjectToSpawnDropDown.gameObject.SetActive(false);
    }

    private void SpawnObject(ulong ownerId, int selection)
    {
        if (!IsServer)
        {
            return;
        }
        var networkObject = NetworkManager.SpawnManager.InstantiateAndSpawn(m_OptionToNetworkObject[selection], ownerId);
        var genericObjectBehaviour = networkObject.GetComponent<GenericNetworkObjectBehaviour>();
        if (genericObjectBehaviour != null)
        {
            float ang = Random.Range(0.0f, 2 * Mathf.PI);
            genericObjectBehaviour.SetDirectionAndVelocity(new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)), Random.Range(2.0f, 5.0f));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnObjectServerRpc(int selection, ServerRpcParams serverRpcParams = default)
    {
        SpawnObject(serverRpcParams.Receive.SenderClientId, selection);
    }

    public void OnSpawnObjectClick()
    {
        if (!IsSpawned || ObjectToSpawnDropDown == null || PrefabsToSpawn.Count == 0)
        {
            return;
        }

        if (IsServer)
        {
            SpawnObject(NetworkManager.ServerClientId, ObjectToSpawnDropDown.value);
        }
        else
        {
            SpawnObjectServerRpc(ObjectToSpawnDropDown.value);
        }
    }

    public override void OnNetworkSpawn()
    {
        SpawnObjectButton?.gameObject.SetActive(true);
        ObjectToSpawnDropDown?.gameObject.SetActive(true);

        if (ObjectToSpawnDropDown == null)
        {
            return;
        }

        foreach (var prefab in PrefabsToSpawn)
        {
            ObjectToSpawnDropDown.options.Add(new Dropdown.OptionData(prefab.name));
            m_OptionToNetworkObject.Add(ObjectToSpawnDropDown.options.Count - 1, prefab);
        }
        ObjectToSpawnDropDown.onValueChanged.AddListener(new UnityAction<int>(OnValueChanged));
        ObjectToSpawnDropDown.value = 0;
        ObjectToSpawnDropDown.Select();
        UpdateItemText();
        base.OnNetworkSpawn();
    }

    private void UpdateItemText()
    {
        ObjectToSpawnDropDown.itemText.text = m_OptionToNetworkObject[ObjectToSpawnDropDown.value].name;
    }

    private void OnValueChanged(int value)
    {
        UpdateItemText();
    }

    public override void OnNetworkDespawn()
    {
        ObjectToSpawnDropDown.ClearOptions();
        m_OptionToNetworkObject.Clear();
        base.OnNetworkDespawn();
    }
}
