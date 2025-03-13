using Unity.Netcode;
using UnityEngine;

public class LookAtInSceneObjectExtension : CameraViewExtension
{
    public GameObject InSceneObjectToLookAt;


    protected override void OnStatusUpdate(ConnectionStates previousState, ConnectionStates currentState)
    {
        if (!InSceneObjectToLookAt)
        {
            return;
        }

        if (currentState == ConnectionStates.Connected)
        {
            var inSceneObjectExtension = InSceneObjectToLookAt.GetComponent<InSceneObjectExtension>();
            inSceneObjectExtension.NetworkObjectStatusUpdate += NetworkObjectStatusUpdate;

            var networkObject = InSceneObjectToLookAt.GetComponent<NetworkObject>();
            if (networkObject && networkObject.IsSpawned)
            {
                LookAtInScenePlacedObject(networkObject);
            }
        }
        else if (previousState == ConnectionStates.Connected && currentState == ConnectionStates.None)
        {
            var inSceneObjectExtension = InSceneObjectToLookAt.GetComponent<InSceneObjectExtension>();
            inSceneObjectExtension.NetworkObjectStatusUpdate -= NetworkObjectStatusUpdate;
        }
        base.OnStatusUpdate(previousState, currentState);
    }

    private void LookAtInScenePlacedObject(NetworkObject networkObject)
    {
        if (!m_Camera || !networkObject)
        {
            return;
        }
        m_Camera.transform.LookAt(networkObject.transform);
    }

    private void NetworkObjectStatusUpdate(NetworkObject networkObject, NetworkObjectStatus status)
    {
        if (status == NetworkObjectStatus.Spawned)
        {
            LookAtInScenePlacedObject(networkObject);
        }
        else
        {
            ResetCamera();
        }
    }
}
