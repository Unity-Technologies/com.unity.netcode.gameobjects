using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace TestProject.ManualTests
{
    public class PlayerPositionOffset : NetworkBehaviour
    {
        [SerializeField]
        private Text m_ServerAuthText;
        [SerializeField]
        private Text m_OwnerAuthText;

        // This creates the normalized vectors to offset each newly spawned instance in a "circular" pattern around the host instance
        private Vector3[] m_Positions = new Vector3[] { Vector3.right, Vector3.right + Vector3.forward, Vector3.forward, Vector3.forward + Vector3.left, Vector3.left, Vector3.back + Vector3.left, Vector3.back, Vector3.back + Vector3.right };
        private static uint s_PositionIndex;
        private const float k_Spacing = 64;
        private static uint s_Layers = 1;
        //[Layer Index][Position Index][Assigned Client]
        private static Dictionary<uint, Dictionary<uint, ulong>> s_ClientPositions = new Dictionary<uint, Dictionary<uint, ulong>>();

        private void RemoveClientOffset(ulong cliendId)
        {
            foreach (var layerEntry in s_ClientPositions)
            {
                var keyToRemove = -1;
                foreach (var positionEntry in layerEntry.Value)
                {
                    if (positionEntry.Value == cliendId)
                    {
                        keyToRemove = (int)positionEntry.Key;
                        break;
                    }
                }
                if (keyToRemove != -1)
                {
                    layerEntry.Value.Remove((uint)keyToRemove);
                    // For next client that joins, start at the removed position index
                    // and layer only if the entry's position index or layer is less than
                    // or equal to the current position and layer values
                    if ((uint)keyToRemove <= s_PositionIndex && layerEntry.Key <= s_Layers)
                    {
                        s_PositionIndex = (uint)keyToRemove;
                        s_Layers = layerEntry.Key;
                    }
                }
            }
        }

        private bool CanAssignClient(ulong clientId)
        {
            if (s_ClientPositions.ContainsKey(s_Layers))
            {
                if (s_ClientPositions[s_Layers].ContainsKey(s_PositionIndex))
                {
                    return s_ClientPositions[s_Layers][s_PositionIndex] == 0;
                }
            }
            return true;
        }

        private void RecordClientOffset(ulong clientId)
        {
            while (!CanAssignClient(clientId))
            {
                IncrementOffset();
            }
            if (!s_ClientPositions.ContainsKey(s_Layers))
            {
                s_ClientPositions.Add(s_Layers, new Dictionary<uint, ulong>());
            }
            if (!s_ClientPositions[s_Layers].ContainsKey(s_PositionIndex))
            {
                s_ClientPositions[s_Layers].Add(s_PositionIndex, clientId);
            }
            else
            {
                s_ClientPositions[s_Layers][s_PositionIndex] = clientId;
            }
        }

        private void IncrementOffset()
        {
            s_PositionIndex++;
            s_PositionIndex = (uint)(s_PositionIndex % m_Positions.Length);

            // This means we rolled over
            if (s_PositionIndex == 0)
            {
                s_Layers++;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                if (IsOwner)
                {
                    // Make sure we start with fresh values when host instantiates
                    s_Layers = 1;
                    s_PositionIndex = 0;
                    s_ClientPositions.Clear();

                    // Host always is at the center
                    transform.position = Vector3.zero;
                    NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
                }
                else
                {
                    RecordClientOffset(OwnerClientId);
                    transform.position = m_Positions[s_PositionIndex] * s_Layers * k_Spacing;
                }
            }

            m_ServerAuthText.text = $"ID-{OwnerClientId}";
            m_OwnerAuthText.text = $"ID-{OwnerClientId}";

            base.OnNetworkSpawn();
        }

        private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
        {
            if (clientId != NetworkManager.LocalClientId)
            {
                RemoveClientOffset(clientId);
            }
        }
    }
}
