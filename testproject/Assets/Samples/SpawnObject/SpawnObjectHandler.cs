using System.Collections.Generic;
using TestProject.ManualTests;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SpawnObjectHandler : NetworkBehaviour
{
    public Button AutoSpawnObjectButton;
    public Button ManualSpawnObjectButton;
    public Dropdown ObjectToSpawnDropDown;
    public List<NetworkObject> PrefabsToSpawn;

    private Dictionary<int, NetworkObject> m_OptionToNetworkObject = new Dictionary<int, NetworkObject>();

    private Vector3 m_SpawnOffset = new Vector3(0.0f, 0.5f, 0.0f);

    private void Start()
    {
        AutoSpawnObjectButton?.gameObject.SetActive(false);
        ManualSpawnObjectButton?.gameObject.SetActive(false);
        ObjectToSpawnDropDown.gameObject.SetActive(false);
    }

    private void SetMotion(GameObject gameObject)
    {
        var genericObjectBehaviour = gameObject.GetComponent<GenericNetworkObjectBehaviour>();
        if (genericObjectBehaviour != null)
        {
            float ang = Random.Range(0.0f, 2 * Mathf.PI);
            genericObjectBehaviour.SetDirectionAndVelocity(new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)), Random.Range(2.0f, 5.0f));
        }
    }

    /// <summary>
    /// Provides an alternate example of how to spawn objects while taking prefab overrides into consideration.
    /// </summary>
    private void ManualSpawnObject(ulong ownerId, int selection)
    {
        var prefabToUse = NetworkManager.GetNetworkPrefabOverride(m_OptionToNetworkObject[selection].gameObject);
        if (prefabToUse != null)
        {
            var instance = Instantiate(prefabToUse);
            instance.transform.position += m_SpawnOffset;
            var instanceNetworkObject = instance.GetComponent<NetworkObject>();
            instanceNetworkObject.SpawnWithOwnership(ownerId);
            SetMotion(instance);
        }
    }

    /// <summary>
    /// Provides an example of using <see cref="NetworkSpawnManager.InstantiateAndSpawn(NetworkObject, ulong, bool, bool, Vector3, Quaternion)"/> that
    /// automatically determines the correct prefab to instantiate and spawn while also taking any prefab overrides into consideration.
    /// Compare the steps of using this method to the <see cref="ManualSpawnObject(ulong, int)"/> script.
    /// </summary>
    private void AutoSpawnObject(ulong ownerId, int selection)
    {
        if (!IsServer)
        {
            return;
        }

        var networkObject = NetworkManager.SpawnManager.InstantiateAndSpawn(m_OptionToNetworkObject[selection], ownerId, position: m_SpawnOffset);
        SetMotion(networkObject.gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AutoSpawnObjectServerRpc(int selection, ServerRpcParams serverRpcParams = default)
    {
        AutoSpawnObject(serverRpcParams.Receive.SenderClientId, selection);
    }

    public void AutoSpawnClick()
    {
        if (!IsSpawned || ObjectToSpawnDropDown == null || PrefabsToSpawn.Count == 0)
        {
            return;
        }

        if (IsServer)
        {
            AutoSpawnObject(NetworkManager.ServerClientId, ObjectToSpawnDropDown.value);
        }
        else
        {
            AutoSpawnObjectServerRpc(ObjectToSpawnDropDown.value);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ManualSpawnObjectServerRpc(int selection, ServerRpcParams serverRpcParams = default)
    {
        AutoSpawnObject(serverRpcParams.Receive.SenderClientId, selection);
    }

    public void ManualSpawnClick()
    {
        if (!IsSpawned || ObjectToSpawnDropDown == null || PrefabsToSpawn.Count == 0)
        {
            return;
        }

        if (IsServer)
        {
            ManualSpawnObject(NetworkManager.ServerClientId, ObjectToSpawnDropDown.value);
        }
        else
        {
            ManualSpawnObjectServerRpc(ObjectToSpawnDropDown.value);
        }
    }

    public override void OnNetworkSpawn()
    {
        AutoSpawnObjectButton?.gameObject.SetActive(true);
        ManualSpawnObjectButton?.gameObject.SetActive(true);
        ObjectToSpawnDropDown?.gameObject.SetActive(true);

        if (ObjectToSpawnDropDown == null)
        {
            return;
        }
        ObjectToSpawnDropDown.ClearOptions();
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
