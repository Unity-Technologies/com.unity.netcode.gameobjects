using Unity.Netcode;
using UnityEngine;

// This should work but doesn't
public class NotStripped : NetworkBehaviour
{
    const float k_PingIntervalSeconds = 0.1f;
    float m_LastPingTime;
    int someCount;

    void FixedUpdate()
    {
        if (!IsServer && IsSpawned)
        {
            if (Time.realtimeSinceStartup - m_LastPingTime > k_PingIntervalSeconds)
            {
                // We could have had a ping/pong where the ping sends the pong and the pong sends the ping. Issue with this
                // is the higher the latency, the lower the sampling would be. We need pings to be sent at a regular interval
                PingServerRPC(someCount++);
                m_LastPingTime = Time.realtimeSinceStartup;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void PingServerRPC(int someParam)
    {
        PongClientRPC(someParam);
    }

    [ClientRpc]
    void PongClientRPC(int someParam)
    {
        Debug.Log("pong");
    }
}
