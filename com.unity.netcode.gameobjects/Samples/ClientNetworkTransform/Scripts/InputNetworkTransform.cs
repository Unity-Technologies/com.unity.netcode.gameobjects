using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode.Samples
{
    // server driven network transform with client side input sending
    public class InputNetworkTransform : NetworkTransform
    {
        public float Speed = 5;

        private Vector3 m_CurrentDirection;

        [ServerRpc]
        public void SendInputServerRpc(Vector3 delta)
        {
            m_CurrentDirection = delta;
        }

        protected override void Update()
        {
            if (CanCommitToTransform)
            {
                transform.position += m_CurrentDirection.normalized * Speed * Time.deltaTime;
            }
            base.Update();
        }

        private void FixedUpdate()
        {
            if (!CanCommitToTransform)
            {
                SendInputServerRpc(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")));
            }
        }
    }
}
