using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class PlayerPositionOffset : NetworkBehaviour
    {
        private static Vector3 s_Offset = Vector3.zero;
        private static Vector3 s_OffsetAmount = Vector3.right * 45;

        [SerializeField]
        private Text m_ServerAuthText;
        [SerializeField]
        private Text m_OwnerAuthText;


        private string m_OriginalServerAuthText;
        private string m_OriginalOwnerAuthText;
        private void Awake()
        {
            m_OriginalServerAuthText = m_ServerAuthText.text;
            m_OriginalOwnerAuthText = m_OwnerAuthText.text;
        }

        public override void OnNetworkSpawn()
        {
            if (string.IsNullOrEmpty(m_OriginalServerAuthText))
            {
                m_OriginalServerAuthText = m_ServerAuthText.text;
            }

            if (string.IsNullOrEmpty(m_OriginalOwnerAuthText))
            {
                m_OriginalOwnerAuthText = m_OwnerAuthText.text;
            }

            if (IsServer)
            {
                transform.position = s_Offset;
                s_Offset += s_OffsetAmount;
                s_OffsetAmount *= -1.0f;
            }

            m_ServerAuthText.text = $"({NetworkManager.LocalClientId})-{m_OriginalServerAuthText}";
            m_OwnerAuthText.text = $"({NetworkManager.LocalClientId})-{m_OriginalOwnerAuthText}";

            base.OnNetworkSpawn();
        }
    }
}
