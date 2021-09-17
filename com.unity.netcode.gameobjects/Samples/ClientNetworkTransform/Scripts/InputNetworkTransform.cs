using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode.Samples
{
    // server driven network transform with client side input sending
    // This shows a simple way to control a server driven transform from a client. You should add your own game logic for movements
    public class InputNetworkTransform : NetworkTransform
    {
        public float Speed { get; set; } = 5;

        private Vector3 m_CurrentDirection;

        private const int k_HorizontalPositiveBit = 0;
        private const int k_HorizontalNegativeBit = 1;
        private const int k_VerticalPositiveBit = 2;
        private const int k_VerticalNegativeBit = 3;

        private byte m_PreviousBitfield = 0;

        [ServerRpc]
        public void SendInputServerRpc(byte delta)
        {
            int horizontal = (delta & (1 << k_HorizontalPositiveBit)) != 0 ? 1 : (delta & (1 << k_HorizontalNegativeBit)) != 0 ? -1 : 0;
            int vertical = (delta & (1 << k_VerticalPositiveBit)) != 0 ? 1 : (delta & (1 << k_VerticalNegativeBit)) != 0 ? -1 : 0;
            m_CurrentDirection = new Vector3(horizontal, 0, vertical);
        }

        protected override void Update()
        {
            if (CanCommitToTransform)
            {
                // you should have your own custom movement logic here
                // this is resistant to jitter, since the current direction is cached. This way, if we receive jittery inputs, this update still knows what to do
                // An improvement could be to do input decay, and slowly decrease that direction over time if no new inputs. This is useful for when a client disconnects for example, so we don't
                // have objects moving forever.
                // This doesn't "impose" a position on the server from clients (which makes that client have authority), we’re making the client “suggest”
                // a pos change, but the server could also do what it wants with that transform in between inputs
                transform.position += m_CurrentDirection.normalized * Speed * Time.deltaTime;
            }
            base.Update();
        }

        private void FixedUpdate()
        {
            if (!CanCommitToTransform && IsOwner)
            {
                byte bitfield = 0;
                if (Input.GetAxis("Horizontal") > 0) { bitfield = (byte)(bitfield | (1 << k_HorizontalPositiveBit)); }
                else if (Input.GetAxis("Horizontal") < 0) { bitfield = (byte)(bitfield | (1 << k_HorizontalNegativeBit)); }
                if (Input.GetAxis("Vertical") > 0) { bitfield = (byte)(bitfield | (1 << k_VerticalPositiveBit)); }
                else if (Input.GetAxis("Vertical") < 0) { bitfield = (byte)(bitfield | (1 << k_VerticalNegativeBit)); }

                if (m_PreviousBitfield != bitfield)
                {
                    SendInputServerRpc(bitfield);
                    m_PreviousBitfield = bitfield;
                }
            }
        }
    }
}
