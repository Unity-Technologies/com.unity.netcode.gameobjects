using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField]
    private float m_Speed = 4.0f;
    [SerializeField]
    private float m_RotSpeed = 1.0f;

    private Rigidbody m_Rigidbody;

    public static Dictionary<ulong, PlayerMovement> Players = new Dictionary<ulong, PlayerMovement>();

    private float m_TickFrequency;
    private float m_DelayInputForTeleport;

    private bool m_IsTeleporting;
    public bool IsTeleporting
    {
        get
        {
            return m_IsTeleporting;
        }
    }

    private Quaternion m_PreviousRotation;
    private RigidbodyInterpolation m_OrginalRigidbodyInterpolation;

    public void Telporting(Vector3 destination)
    {
        if (IsSpawned && IsServer && !m_IsTeleporting)
        {
            m_IsTeleporting = true;
            m_DelayInputForTeleport = Time.realtimeSinceStartup + (2 * m_TickFrequency);
            m_Rigidbody.isKinematic = true;
            m_OrginalRigidbodyInterpolation = m_Rigidbody.interpolation;
            m_Rigidbody.interpolation = RigidbodyInterpolation.None;
            // Since the player-cube is a cube, when colliding with something it could
            // cause the cube to rotate based on the surface being collided against
            // and the facing of the cube. This prevents rotation from being changed
            // due to colliding with a side wall (and then teleported)
            transform.rotation = m_PreviousRotation;

            // Now teleport
            GetComponent<NetworkTransform>().Teleport(destination, transform.rotation, transform.localScale);
        }
    }

    public override void OnNetworkSpawn()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        if (IsServer)
        {
            m_TickFrequency = 1.0f / NetworkManager.NetworkTickSystem.TickRate;
            var temp = transform.position;
            temp.y = 0.5f;
            transform.position = temp;
        }
        base.OnNetworkSpawn();
        Players[OwnerClientId] = this; // todo should really have a NetworkStop for unregistering this...
    }

    private void MovePlayer(float vertical, float horizontal, float timeDelta)
    {
        if (m_IsTeleporting)
        {
            return;
        }

        if (Mathf.Abs(vertical) > 0.001f)
        {
            var position = transform.position;
            position += vertical * transform.forward * m_Speed * timeDelta;
            m_Rigidbody.MovePosition(position);
        }

        if (Mathf.Abs(horizontal) > 0.001f)
        {
            var rotation = Quaternion.Euler(0, horizontal * 90 * m_RotSpeed * timeDelta, 0) * transform.rotation;
            m_Rigidbody.rotation = rotation;
        }
    }

    [ServerRpc]
    private void MovePlayerServerRpc(MovePlayerData movePlayerData)
    {
        m_CurrentMoveState = m_MovePlayerState.UpdateFromData(movePlayerData);
    }

    private MovePlayerData m_MovePlayerData = new MovePlayerData();
    private MovePlayerState m_MovePlayerState = new MovePlayerState();
    private Vector2 m_CurrentMoveState;

    private void LateUpdate()
    {
        if (!IsSpawned || m_IsTeleporting)
        {
            return;
        }
        if (IsOwner)
        {
            m_MovePlayerData.SetInputState();

            if (IsServer)
            {
                m_CurrentMoveState = m_MovePlayerState.UpdateFromData(m_MovePlayerData);
            }
            else
            {
                if (m_MovePlayerData.HasKeyStates)
                {
                    MovePlayerServerRpc(m_MovePlayerData);
                    m_MovePlayerState.HadKeyStates = true;
                }
                else if (m_MovePlayerState.HadKeyStates)
                {
                    MovePlayerServerRpc(m_MovePlayerData);
                    m_MovePlayerState.ResetKeyStates(m_MovePlayerData);
                }
            }
        }
    }


    private void FixedUpdate()
    {
        if (!IsSpawned || !IsServer)
        {
            return;
        }

        if (m_IsTeleporting)
        {
            if (Time.realtimeSinceStartup >= m_DelayInputForTeleport)
            {
                m_MovePlayerState.ResetKeyStates(m_MovePlayerData);
                m_IsTeleporting = false;
                m_Rigidbody.isKinematic = false;
                m_Rigidbody.interpolation = m_OrginalRigidbodyInterpolation;
            }
            else
            {
                return;
            }
        }

        MovePlayer(m_CurrentMoveState.y, m_CurrentMoveState.x, Time.deltaTime);
        // This allows us to rollback to the previous rotation for the teleport
        // sample. If we don't do this the box will collide with the wall and
        // will slightly rotate the box to align with the wall
        m_PreviousRotation = transform.rotation;
    }

    /// <summary>
    /// An INetworkSerializable implementation to handle server authoritative motion
    /// Uses the indices the key codes are registered as bit positions to send a single uint packed
    /// </summary>
    public class MovePlayerData : INetworkSerializable
    {
        private List<KeyCode> m_KeyCodes = new List<KeyCode>() { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };

        private uint m_KeyStates;

        public bool HasKeyStates
        {
            get
            {
                return m_KeyStates > 0;
            }
        }

        public void ResetKeyStates()
        {
            m_KeyStates = 0;
        }

        public List<KeyCode> KeyCodesPressed = new List<KeyCode>();

        private const int k_MaxKeys = sizeof(uint) * 8;

        public void SetInputState()
        {
            if (m_KeyCodes.Count >= k_MaxKeys)
            {
                throw new System.Exception($"Number of registered keys to track ({m_KeyCodes.Count}) exceeded maximum ({k_MaxKeys})");
            }

            m_KeyStates = 0;
            for (int i = 0; i < m_KeyCodes.Count; i++)
            {
                if (Input.GetKey(m_KeyCodes[i]))
                {
                    m_KeyStates |= (uint)(1 << i);
                }
            }
        }

        public void GetInputState()
        {
            KeyCodesPressed.Clear();
            for (int i = 0; i < m_KeyCodes.Count; i++)
            {
                if ((m_KeyStates & (uint)(1 << i)) > 0)
                {
                    KeyCodesPressed.Add(m_KeyCodes[i]);
                }
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsWriter)
            {
                var writer = serializer.GetFastBufferWriter();
                BytePacker.WriteValuePacked(writer, m_KeyStates);
            }
            else
            {
                var reader = serializer.GetFastBufferReader();
                ByteUnpacker.ReadValuePacked(reader, out m_KeyStates);
            }
        }
    }

    /// <summary>
    /// Used to maintain the stats of the player's motion
    /// </summary>
    public class MovePlayerState
    {
        private float m_HorizontalAccel;
        private float m_VerticalAccel;
        private Vector2 m_XYInput;
        public List<KeyCode> KeyCodesPressed = new List<KeyCode>();

        public bool HadKeyStates;

        public void ResetKeyStates(MovePlayerData movePlayerData)
        {
            movePlayerData.ResetKeyStates();
            KeyCodesPressed.Clear();
            m_HorizontalAccel = 0;
            m_VerticalAccel = 0;
            m_XYInput = Vector2.zero;
            HadKeyStates = false;
        }
        public Vector2 UpdateAxis(bool decay = false, float lerpDecay = 0.15f)
        {
            m_XYInput.x = m_HorizontalAccel;
            m_XYInput.y = m_VerticalAccel;
            return m_XYInput;
        }

        public Vector2 UpdateFromData(MovePlayerData movePlayerData)
        {
            movePlayerData.GetInputState();

            if (HadKeyStates && movePlayerData.KeyCodesPressed.Count == 0)
            {
                ResetKeyStates(movePlayerData);
            }
            else
            {
                UpdateAxialAcceleration(movePlayerData.KeyCodesPressed);
            }

            return UpdateAxis();
        }

        private void UpdateAxialAcceleration(List<KeyCode> keyCodesPressed)
        {
            KeyCodesPressed = keyCodesPressed;
            m_VerticalAccel = Mathf.Lerp(m_VerticalAccel, 0.0f, 0.25f);
            m_HorizontalAccel = Mathf.Lerp(m_HorizontalAccel, 0.0f, 0.25f);
            HadKeyStates = keyCodesPressed.Count > 0;
            foreach (var keyPressed in KeyCodesPressed)
            {
                switch (keyPressed)
                {
                    case KeyCode.UpArrow:
                        {
                            m_VerticalAccel = 1.0f;
                            break;
                        }
                    case KeyCode.DownArrow:
                        {
                            m_VerticalAccel = -1.0f;
                            break;
                        }
                    case KeyCode.RightArrow:
                        {
                            m_HorizontalAccel = 1.0f;
                            break;
                        }
                    case KeyCode.LeftArrow:
                        {
                            m_HorizontalAccel = -1.0f;
                            break;
                        }
                }
            }
        }
    }
}
