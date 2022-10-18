
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

    public bool DebugEulerInterpolation;
    private NetworkVariable<bool> m_DebugEulerInterpolation = new NetworkVariable<bool>();

    [Range(0,3)]
    public int DebugWheelIndex;

    private NetworkVariable<int> m_WheelIndexToDebug = new NetworkVariable<int>();

    private NetworkVariable<NetworkBehaviourReference> m_WheelToDebug = new NetworkVariable<NetworkBehaviourReference>();

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

            m_DebugEulerInterpolation.Value = DebugEulerInterpolation;
            m_WheelIndexToDebug.Value = DebugWheelIndex;

            m_AssetInstance.Spawn();

            var wheelToDebug = m_MockCarParts.Wheels[DebugWheelIndex];
            m_WheelToDebug.Value = new NetworkBehaviourReference(m_WheelNetworkTransforms[wheelToDebug]);
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
        if (!IsSpawned)
        {
            return;
        }

        if (IsServer)
        {
            if (m_WheelIndexToDebug.Value != DebugWheelIndex)
            {
                m_WheelIndexToDebug.Value = DebugWheelIndex;
                var wheel = m_MockCarParts.Wheels[DebugWheelIndex];
                m_WheelToDebug.Value = new NetworkBehaviourReference(m_WheelNetworkTransforms[wheel]);
            }

            if (m_DebugEulerInterpolation.Value != DebugEulerInterpolation)
            {
                m_DebugEulerInterpolation.Value = DebugEulerInterpolation;
            }
        }
        else
        if (!IsServer && m_DebugEulerInterpolation.Value)
        {
            var networkTransform = (NetworkTransform)null;
            if (m_WheelToDebug.Value.TryGet(out networkTransform))
            {
                WheelDebugInfoServerRpc(networkTransform.transform.localEulerAngles, networkTransform.transform.eulerAngles);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void WheelDebugInfoServerRpc(Vector3 localEuler, Vector3 euler)
    {
        var index = m_WheelIndexToDebug.Value;
        var wheel = m_MockCarParts.Wheels[index];
        var localTransform = m_WheelNetworkTransforms[wheel].transform;
        Debug.Log($"[Wheel-{DebugWheelIndex}] LocalEuler-Client {localEuler} vs LocalEuler-Server {localTransform.localEulerAngles} | Euler-Client {euler} vs Euler-Server {localTransform.eulerAngles}");
    }

    private void FixedUpdate()
    {
        if (!IsServer || !IsSpawned)
        {
            return;
        }
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
