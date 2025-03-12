using Unity.Netcode;
using UnityEngine;

public class InSceneObjectExtension : BaseNetcodeExtension
{
    public bool StartDespawned;
    public GameObject VisualAndInteractionsNode;

    // Determines if this is the first time this NetworkObject has been spawned
    // for the current network session.
    private bool m_IsFirstSpawn = true;

    protected override void OnAwake()
    {
        // Always start with the visual and interactive portions disabled
        VisualAndInteractionsNode?.SetActive(false);
        base.OnAwake();
    }

    protected override void OnStatusUpdate(ConnectionStates previousState, ConnectionStates currentState)
    {
        if (previousState == ConnectionStates.Connected && currentState == ConnectionStates.None)
        {
            m_IsFirstSpawn = true;
        }
        base.OnStatusUpdate(previousState, currentState);
    }

    protected override void OnNetworkPostSpawn()
    {
        VisualAndInteractionsNode?.SetActive(true);
        if (IsAuthorityInstance())
        {
            if (StartDespawned && m_IsFirstSpawn)
            {
                NetworkObject.Despawn(false);
            }
            m_IsFirstSpawn = false;
        }
        base.OnNetworkPostSpawn();
    }

    public override void OnNetworkDespawn()
    {
        VisualAndInteractionsNode?.SetActive(false);
        base.OnNetworkDespawn();
    }

    private Rect TopRightGUI(Rect totalRectSize)
    {
        if (m_ConnectionState == ConnectionStates.Connected)
        {
            if (IsAuthorityInstance())
            {
                var buttonLabel = IsSpawned ? "Despawn" : "Spawn";
                var retButtonValues = Draw.Button(totalRectSize, $"{buttonLabel} {name}");
                if (retButtonValues.Item2)
                {
                    if (IsSpawned)
                    {
                        NetworkObject.Despawn(false);
                    }
                    else
                    {
                        NetworkObject.Spawn();
                    }
                }
                totalRectSize = retButtonValues.Item1;
            }
        }
        else
        {
            var retToggleValues = Draw.Toggle(totalRectSize, StartDespawned, "Start Despawned");
            StartDespawned = retToggleValues.Item2;
            totalRectSize = retToggleValues.Item1;
        }

        return totalRectSize;
    }
    protected override Rect OnGUIUpdate(Rect totalRectSize, ScreenSpaceRegions screenSpaceRegion)
    {
        switch (screenSpaceRegion)
        {
            case ScreenSpaceRegions.TopRight:
                {
                    totalRectSize = TopRightGUI(totalRectSize);
                    break;
                }
        }
        return base.OnGUIUpdate(totalRectSize, screenSpaceRegion);
    }
}
