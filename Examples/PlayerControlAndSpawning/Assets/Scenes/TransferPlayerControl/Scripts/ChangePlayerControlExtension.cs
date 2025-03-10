using Unity.Netcode;
using UnityEngine;

public class ChangePlayerControlExtension : BaseObjectSpawnExtension
{
    public KeyCode PrevClientKeyCode = KeyCode.LeftBracket;
    public KeyCode NextClientKeyCode = KeyCode.RightBracket;
    private int m_CurrentFollowPlayerIndex = 0;

    private NetworkVariable<int> m_FollowPlayerIndex = new NetworkVariable<int>();

    protected override void OnInitialize()
    {
        m_ExtendedNetworkManager.OnConnectionEvent += OnConnectionEvent;
        base.OnInitialize();
    }

    private void OnConnectionEvent(NetworkManager arg1, ConnectionEventData eventData)
    {
        if (!IsAuthorityInstance())
        {
            return;
        }
        // If the current owner disconnects...
        if (eventData.EventType == ConnectionEvent.PeerDisconnected)
        {
            var spawnedObject = GetSpawnedNetworkObject();
            // and if we have spawned an object to transfer control...
            if (spawnedObject && eventData.ClientId == spawnedObject.OwnerClientId)
            {
                // the authority of this session takes ownership.
                spawnedObject.ChangeOwnership(OwnerClientId);
            }
        }
    }

    private void ChangeClientOwner()
    {
        bool previousClient = Input.GetKeyDown(PrevClientKeyCode);
        bool nextClient = Input.GetKeyDown(NextClientKeyCode);

        if ((previousClient || nextClient) && NetworkManager.ConnectedClientsIds.Count > 0)
        {
            if (previousClient)
            {
                m_CurrentFollowPlayerIndex--;
                if (m_CurrentFollowPlayerIndex < 0)
                {
                    m_CurrentFollowPlayerIndex = NetworkManager.ConnectedClientsIds.Count - 1;
                }
            }
            else
            {
                m_CurrentFollowPlayerIndex++;
            }

            m_CurrentFollowPlayerIndex %= NetworkManager.ConnectedClientsIds.Count;

            var playerId = NetworkManager.ConnectedClientsIds[m_CurrentFollowPlayerIndex];
            if (playerId != GetSpawnedNetworkObject().OwnerClientId)
            {
                GetSpawnedNetworkObject().ChangeOwnership(playerId);
                m_FollowPlayerIndex.Value = m_CurrentFollowPlayerIndex;
            }
        }
    }

    private void UpdateFollowPlayerIndex(ulong playerId)
    {
        for (int i = 0; i < NetworkManager.ConnectedClientsIds.Count; i++)
        {
            if (NetworkManager.ConnectedClientsIds[i] == playerId)
            {
                m_CurrentFollowPlayerIndex = i;
                m_FollowPlayerIndex.Value = m_CurrentFollowPlayerIndex;
            }
        }
    }

    protected override void SpawnObject(ulong playerId, bool isPlayerObject)
    {
        base.SpawnObject(playerId, isPlayerObject);
        GetSpawnedNetworkObject().GetComponent<OwnershipChangeNonAuthorityExtension>().OwnershipChanged += SpawnedObjectOwnershipChanged;
    }

    private void SpawnedObjectOwnershipChanged(ulong previous, ulong current)
    {
        if (HasAuthority)
        {
            UpdateFollowPlayerIndex(current);
        }
    }

    protected override void OnObjectDespawned()
    {
        if (IsAuthorityInstance())
        {
            m_CurrentFollowPlayerIndex = 0;
            if (IsSpawned)
            {
                m_FollowPlayerIndex.Value = m_CurrentFollowPlayerIndex;
            }
        }
        base.OnObjectDespawned();
    }

    protected override bool CanDespawnObject()
    {
        return base.CanDespawnObject() && GetSpawnedNetworkObject().HasAuthority;
    }

    protected override void OnGeneralUpdate()
    {
        if (IsAuthorityInstance())
        {
            ChangeClientOwner();
        }

        base.OnGeneralUpdate();
    }

    protected override Rect OnGUIUpdate(Rect totalRectSize, ScreenSpaceRegions screenSpaceRegion)
    {
        if (!IsSpawned || m_ApplicationExitPending)
        {
            return totalRectSize;
        }
        switch (screenSpaceRegion)
        {
            case ScreenSpaceRegions.TopRight:
                {
                    if (m_ConnectionState == ConnectionStates.Connected && IsAuthorityInstance())
                    {
                        totalRectSize = Draw.Label(totalRectSize, $"[{PrevClientKeyCode}] Previous Client");
                        totalRectSize = Draw.Label(totalRectSize, $"[{NextClientKeyCode}] Next Client");
                    }
                    break;
                }
            case ScreenSpaceRegions.TopLeft:
                {
                    if (m_ConnectionState == ConnectionStates.Connected && GetSpawnedNetworkObject())
                    {
                        totalRectSize = Draw.Label(totalRectSize, $"Client-{GetSpawnedNetworkObject().OwnerClientId} is in control.");
                    }
                    break;
                }
        }
        return base.OnGUIUpdate(totalRectSize, screenSpaceRegion);
    }
}
