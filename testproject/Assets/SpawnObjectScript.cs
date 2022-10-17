
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class SpawnObjectScript : NetworkBehaviour
{
    public GameObject AssetToSpawn;

    [Range(0.0f, 3.0f)]
    public float CarRotationSpeed = 0.1f;

    [Range(0.0f, 30.0f)]
    public float WheelRotationSpeed = 1.0f;

    [Range(0.1f, 5.0f)]
    public float WheelScale = 1.5f;

    public bool UseQuaternionSync;

    public bool Interpolate = true;

    private NetworkObject m_AssetInstance;

    private MockCarParts m_MockCarParts;

    private Dictionary<GameObject, NetworkTransform> m_WheelNetworkTransforms = new Dictionary<GameObject, NetworkTransform>();

    private float m_CurrentWheelScale;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            var instance = Instantiate(AssetToSpawn);
            m_AssetInstance = instance.GetComponent<NetworkObject>();
            m_MockCarParts = instance.GetComponent<MockCarParts>();
            m_CurrentWheelScale = WheelScale;
            foreach (var wheel in m_MockCarParts.Wheels)
            {
                wheel.transform.localScale = Vector3.one * WheelScale;
                var networkTransform = wheel.GetComponent<NetworkTransform>();
                m_WheelNetworkTransforms.Add(wheel, networkTransform);
                networkTransform.UseQuaternionSynch = UseQuaternionSync;
                networkTransform.Interpolate = Interpolate;
            }

            m_AssetInstance.Spawn();
        }
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && m_AssetInstance != null && m_AssetInstance.IsSpawned)
        {
            m_AssetInstance.Despawn(true);
            m_AssetInstance = null;
        }
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (IsServer && IsSpawned)
        {
            m_AssetInstance.transform.Rotate(0.0f, CarRotationSpeed, 0.0f, Space.World);
            foreach (var wheel in m_MockCarParts.Wheels)
            {
                wheel.transform.Rotate(WheelRotationSpeed, 0.0f, 0.0f, Space.Self);

                if (WheelScale != m_CurrentWheelScale)
                {
                    wheel.transform.localScale = Vector3.one * WheelScale;
                }

                m_WheelNetworkTransforms[wheel].UseQuaternionSynch = UseQuaternionSync;
                m_WheelNetworkTransforms[wheel].Interpolate = Interpolate;
            }
            if (WheelScale != m_CurrentWheelScale)
            {
                m_CurrentWheelScale = WheelScale;
            }
        }
    }
}
