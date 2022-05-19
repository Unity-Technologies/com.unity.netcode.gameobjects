using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

#if !UNITY_SERVER
public class Stripped : NetworkBehaviour
{
    const float k_PingIntervalSeconds = 0.1f;
    float m_LastPingTime;
    NetworkVariable<int> asdf = new();
    NetworkVariable<FixedString512Bytes> someString = new();

    void FixedUpdate()
    {
        if (!IsServer && IsSpawned)
        {
            if (Time.realtimeSinceStartup - m_LastPingTime > k_PingIntervalSeconds)
            {
                // We could have had a ping/pong where the ping sends the pong and the pong sends the ping. Issue with this
                // is the higher the latency, the lower the sampling would be. We need pings to be sent at a regular interval
                PingServerRPC();
                m_LastPingTime = Time.realtimeSinceStartup;
            }
        }

        if (IsServer)
        {
            asdf.Value += 1;
            someString.Value = new FixedString512Bytes($"{asdf.Value}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void PingServerRPC()
    {
        PongClientRPC();
    }

    [ClientRpc]
    void PongClientRPC()
    {
        Debug.Log("pong");
    }
}
#endif