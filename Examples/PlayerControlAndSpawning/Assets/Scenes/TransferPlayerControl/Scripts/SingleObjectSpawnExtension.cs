using Unity.Netcode;
using UnityEngine;

public class SingleObjectSpawnExtension : BaseMonoExtension
{
    public GameObject PrefabToSpawn;

    private GameObject m_PrefabInstance;

    protected override void OnStatusUpdate(ConnectionStates previousState, ConnectionStates currentState)
    {
        if (m_ExtendedNetworkManager.IsAuthorityInstance())
        {
            if (currentState == ConnectionStates.Connected && !m_PrefabInstance && PrefabToSpawn)
            {
                NetworkObject.InstantiateAndSpawn(PrefabToSpawn, m_ExtendedNetworkManager, destroyWithScene: false);
            }
            else if (previousState == ConnectionStates.Connected && currentState == ConnectionStates.None)
            {
                m_PrefabInstance = null;
            }
        }
        base.OnStatusUpdate(previousState, currentState);
    }
}
