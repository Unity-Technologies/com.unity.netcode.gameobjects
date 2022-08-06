using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField]
    private float m_Speed = 20.0f;
    [SerializeField]
    private float m_RotSpeed = 5.0f;

    private Rigidbody m_Rigidbody;

    public static Dictionary<ulong, PlayerMovement> Players = new Dictionary<ulong, PlayerMovement>();

    private void Start()
    {

    }

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



    public void Telporting()
    {
        if (IsSpawned && IsServer)
        {
            m_IsTeleporting = true;
            m_DelayInputForTeleport = Time.realtimeSinceStartup + (2 * m_TickFrequency);
            m_Rigidbody.angularVelocity = Vector3.zero;
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.isKinematic = true;
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

    private void MovePlayer(float veritcal, float horizontal, float timeDelta)
    {
        if (m_IsTeleporting)
        {
            return;
        }
        var clampedTimeDelta = Mathf.Clamp(timeDelta, m_TickFrequency * 0.40f, m_TickFrequency);

        var position = transform.position;

        position += veritcal * m_Speed * clampedTimeDelta * transform.forward;

        var rotation = Quaternion.Euler(0, horizontal * 90 * m_RotSpeed * clampedTimeDelta, 0) * transform.rotation;

        m_Rigidbody.MovePosition(position);
        m_Rigidbody.MoveRotation(rotation);
    }

    [ServerRpc]
    private void MovePlayerServerRpc(MovePlayerData movePlayerData)
    {
        var serverTimeDelta = (float)(NetworkManager.ServerTime.Time - movePlayerData.ServerTime);
        //Debug.Log($"ServerTimeDelta: {serverTimeDelta} | Calculated Time Delta: {m_TickFrequency * serverTimeDelta + Time.deltaTime}");
        MovePlayer(movePlayerData.VerticalAxis, movePlayerData.HorizontalAxis, m_TickFrequency * serverTimeDelta - Time.deltaTime);
    }


    private MovePlayerData m_MovePlayerData = new MovePlayerData();
    private void LateUpdate()
    {
        if (IsSpawned && IsOwner)
        {
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
            {
                if (IsServer)
                {
                    MovePlayer(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"), m_TickFrequency * 0.40f);
                }
                else
                {
                    m_MovePlayerData.VerticalAxis = Input.GetAxis("Vertical");
                    m_MovePlayerData.HorizontalAxis = Input.GetAxis("Horizontal");
                    m_MovePlayerData.ServerTime = NetworkManager.ServerTime.Time;
                    MovePlayerServerRpc(m_MovePlayerData);
                }
            }
        }

        if (IsServer)
        {
            if (m_IsTeleporting)
            {
                if (Time.realtimeSinceStartup >= m_DelayInputForTeleport)
                {
                    m_IsTeleporting = false;
                    m_Rigidbody.isKinematic = false;
                }
                else
                {
                    return;
                }
            }
        }
    }

    public class MovePlayerData : INetworkSerializable
    {
        public float HorizontalAxis;
        public float VerticalAxis;
        public double ServerTime;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsWriter)
            {
                var writer = serializer.GetFastBufferWriter();
                BytePacker.WriteValuePacked(writer, HorizontalAxis);
                BytePacker.WriteValuePacked(writer, VerticalAxis);
                BytePacker.WriteValuePacked(writer, ServerTime);
            }
            else
            {
                var reader = serializer.GetFastBufferReader();
                ByteUnpacker.ReadValuePacked(reader, out HorizontalAxis);
                ByteUnpacker.ReadValuePacked(reader, out VerticalAxis);
                ByteUnpacker.ReadValuePacked(reader, out ServerTime);
            }
        }
    }
}
