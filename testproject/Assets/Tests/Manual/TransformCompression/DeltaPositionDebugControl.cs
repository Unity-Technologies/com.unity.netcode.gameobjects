
//using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class DeltaPositionDebugControl : NetworkBehaviour
{

    public bool Interpolate = true;

    public bool DebugDeltaPositionCompression;

    public bool DeltaPositionCompression;

    public bool LocalDeltaPositionDebug;

    public Camera PlayerCamera;
    public Camera MainCamera;

    private int m_CurrentPlayerFollowIndex;

    private NetworkObject m_PlayerObject;
    private PlayerMovement m_PlayerMovement;

    private bool m_DebugDeltaPositionCompression;
    private bool m_LocalDeltaPositionDebug;


    private bool m_Interpolate;

    private void Start()
    {
        m_DebugDeltaPositionCompression = DebugDeltaPositionCompression;
        m_Interpolate = Interpolate;
    }

    public override void OnNetworkSpawn()
    {

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    private void SwitchPlayerCamera(NetworkObject networkObject)
    {
        MainCamera.gameObject.SetActive(false);
        PlayerCamera.gameObject.SetActive(true);
        PlayerCamera.transform.SetParent(networkObject.transform, false);
    }

    private void SwitchToMainCamera()
    {
        PlayerCamera.gameObject.SetActive(false);
        MainCamera.gameObject.SetActive(true);
    }

    [ClientRpc]
    private void SwitchToMainCameraClientRpc()
    {
        SwitchToMainCamera();
    }

    [ClientRpc]
    private void SetFollowPlayerCameraClientRpc(NetworkObjectReference networkObjectReference)
    {
        if (networkObjectReference.TryGet(out NetworkObject networkObject))
        {
            SwitchPlayerCamera(networkObject);
        }
    }

    private void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        if (m_PlayerObject == null)
        {
            m_PlayerObject = NetworkManager.LocalClient.PlayerObject;
            if (m_PlayerObject != null)
            {
                m_PlayerMovement = m_PlayerObject.GetComponent<PlayerMovement>();
                m_PlayerMovement.Speed = 10.0f;
            }
        }

        if (IsServer)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SetFollowPlayerCameraClientRpc(new NetworkObjectReference(NetworkManager.ConnectedClientsList[m_CurrentPlayerFollowIndex].PlayerObject));
                SwitchPlayerCamera(NetworkManager.ConnectedClientsList[m_CurrentPlayerFollowIndex].PlayerObject);
                m_CurrentPlayerFollowIndex++;
                m_CurrentPlayerFollowIndex = m_CurrentPlayerFollowIndex % NetworkManager.ConnectedClientsList.Count;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SwitchToMainCameraClientRpc();
            }

            NetworkTransform.UsePositionDeltaCompression = DeltaPositionCompression;

            if (m_DebugDeltaPositionCompression != DebugDeltaPositionCompression)
            {
                m_DebugDeltaPositionCompression = DebugDeltaPositionCompression;
                SetDebugDeltaPoistionStateClientRpc(DebugDeltaPositionCompression);
            }

            if (Interpolate != m_Interpolate)
            {
                m_Interpolate = Interpolate;
                SetInterpolateClientRpc(Interpolate);
            }

            if (LocalDeltaPositionDebug != m_LocalDeltaPositionDebug)
            {
                if (LocalDeltaPositionDebug)
                {
                    m_PreviousLocalPositionDebug = m_PlayerMovement.transform.position;
                    NetworkManager.NetworkTickSystem.Tick += LocalDebugNetworkTickSystem_Tick;
                }
                else
                {
                    NetworkManager.NetworkTickSystem.Tick -= LocalDebugNetworkTickSystem_Tick;

                }
                m_LocalDeltaPositionDebug = LocalDeltaPositionDebug;
            }
        }
        else
        {
            if (DebugDeltaPositionCompression && m_PlayerMovement != null)
            {
                if (m_PlayerPosition != m_PlayerMovement.transform.position)
                {
                    m_PlayerPosition = m_PlayerMovement.transform.position;
                    PositionDebugInfoServerRpc(m_PlayerPosition);
                }
            }
        }
    }

    private Vector3 m_PreviousLocalPositionDebug = Vector3.zero;
    private Vector3 m_DeltaLocalPositionDebug = Vector3.zero;
    private Vector3 m_DecompLocalPositionDebug = Vector3.zero;

    private void LocalDebugNetworkTickSystem_Tick()
    {
        if((m_PlayerMovement.transform.position - m_PreviousLocalPositionDebug).sqrMagnitude > 0.0001f)
        {
            var currentPosition = m_PlayerMovement.transform.position;
            var compressedDeltaPosition = DeltaPositionCompressor.CompressDeltaPosition(ref m_PreviousLocalPositionDebug, ref currentPosition);
            DeltaPositionCompressor.DecompressDeltaPosition(ref m_DeltaLocalPositionDebug, compressedDeltaPosition);
            m_DecompLocalPositionDebug = m_PreviousLocalPositionDebug + m_DeltaLocalPositionDebug;
            var realDeltaPosition = currentPosition - m_PreviousLocalPositionDebug;
            m_PreviousLocalPositionDebug = currentPosition;

            Debug.Log($"[Decomp] {m_DecompLocalPositionDebug} vs [Current] {currentPosition} | [CalcDelta] {m_DeltaLocalPositionDebug} vs [RealDelta] {realDeltaPosition}");
        }
    }

    private Vector3 m_PlayerPosition = Vector3.zero;

    [ClientRpc]
    private void SetInterpolateClientRpc(bool interpolate)
    {
        if (IsServer)
        {
            return;
        }
        m_PlayerMovement.Interpolate = interpolate;
    }

    private Vector3 m_TargetClientPosition;
    [ServerRpc(RequireOwnership = false)]
    private void PositionDebugInfoServerRpc(Vector3 position, ServerRpcParams serverRpcParams = default)
    {
        m_TargetClientPosition = position;
    }

    [ClientRpc]
    private void SetDebugDeltaPoistionStateClientRpc(bool enable)
    {
        if (IsServer)
        {
            return;
        }
        DebugDeltaPositionCompression = enable;
    }
}
