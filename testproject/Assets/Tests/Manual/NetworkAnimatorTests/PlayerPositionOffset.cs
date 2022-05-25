using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class PlayerPositionOffset : NetworkBehaviour
    {
        [SerializeField]
        private Text m_ServerAuthText;
        [SerializeField]
        private Text m_OwnerAuthText;


        private Vector3[] m_Positions = new Vector3[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        private static uint s_PositionIndex;
        private const float k_Spacing = 64;
        private static float s_Layers = 0;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                if (IsOwner)
                {
                    transform.position = Vector3.zero;
                }
                else
                {
                    if (s_PositionIndex == 0)
                    {
                        s_Layers++;
                    }
                    transform.position = m_Positions[s_PositionIndex] * s_Layers * k_Spacing;
                    s_PositionIndex++;
                    s_PositionIndex = (uint)(s_PositionIndex % m_Positions.Length);
                }
            }

            m_ServerAuthText.text = $"ID-{OwnerClientId}";
            m_OwnerAuthText.text = $"ID-{OwnerClientId}";

            base.OnNetworkSpawn();
        }
    }
}
