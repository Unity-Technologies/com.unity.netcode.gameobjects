using Unity.Netcode;
using UnityEngine;

public class ChangePlayerControlExtension : BaseObjectSpawnExtension
{
    private int m_CurrentFollowPlayerIndex = 0;

    private NetworkVariable<int> m_FollowPlayerIndex = new NetworkVariable<int>();

    private void ChangeClientOwner()
    {
        bool leftBracket = Input.GetKeyDown(KeyCode.LeftBracket);
        bool rightBracket = Input.GetKeyDown(KeyCode.RightBracket);

        if ((leftBracket || rightBracket) && NetworkManager.ConnectedClientsIds.Count > 0)
        {
            if (leftBracket)
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

    protected override void OnObjectDespawned()
    {
        if (m_ExtendedNetworkManager.IsAuthorityInstance())
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
        if (m_ExtendedNetworkManager.IsAuthorityInstance())
        {
            ChangeClientOwner();
        }

        base.OnGeneralUpdate();
    }

    protected override Rect OnGUIUpdate(Rect totalRectSize, ScreenSpaceRegions screenSpaceRegion)
    {
        if (!IsSpawned)
        {
            return totalRectSize;
        }
        switch (screenSpaceRegion)
        {
            case ScreenSpaceRegions.TopRight:
                {
                    if (m_ConnectionState == ConnectionStates.Connected && m_ExtendedNetworkManager.IsAuthorityInstance())
                    {
                        totalRectSize = DrawLabel(totalRectSize, $"([ or ]) changes player control");
                    }
                    break;
                }
            case ScreenSpaceRegions.TopLeft:
                {
                    if (m_ConnectionState == ConnectionStates.Connected && GetSpawnedNetworkObject())
                    {
                        totalRectSize = DrawLabel(totalRectSize, $"Client-{GetSpawnedNetworkObject().OwnerClientId} is in control.");
                    }
                    break;
                }
        }
        return base.OnGUIUpdate(totalRectSize, screenSpaceRegion);
    }
}
