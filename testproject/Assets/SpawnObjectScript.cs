
using UnityEngine;
using Unity.Netcode;

public class SpawnObjectScript : NetworkBehaviour
{
    public GameObject AssetToSpawn;

    [Range(0.0f,30.0f)]
    public float RotationSpeed = 1.0f;

    [Range(0.1f, 5.0f)]
    public float WheelScale = 1.5f;

    private NetworkObject m_AssetInstance;

    private MockCarParts m_MockCarParts;

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
        if (IsSpawned && IsServer)
        {
            var wheelQuatShown = false;
            foreach (var wheel in m_MockCarParts.Wheels)
            {
                wheel.transform.Rotate(RotationSpeed, 0.0f, 0.0f, Space.Self);
                if (!wheelQuatShown)
                {
                    Debug.Log($"Rotation: {wheel.transform.rotation}");
                    wheelQuatShown = true;
                }

                if (WheelScale != m_CurrentWheelScale)
                {
                    wheel.transform.localScale = Vector3.one * WheelScale;
                }
            }
            if (WheelScale != m_CurrentWheelScale)
            {
                m_CurrentWheelScale = WheelScale;
            }
        }
    }
}
