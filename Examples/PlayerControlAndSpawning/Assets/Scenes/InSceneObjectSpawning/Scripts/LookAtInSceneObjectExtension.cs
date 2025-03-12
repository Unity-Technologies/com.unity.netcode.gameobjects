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
        }
        else if (previousState == ConnectionStates.Connected && currentState == ConnectionStates.None)
        {
            var inSceneObjectExtension = InSceneObjectToLookAt.GetComponent<InSceneObjectExtension>();
            inSceneObjectExtension.NetworkObjectStatusUpdate -= NetworkObjectStatusUpdate;
        }
        base.OnStatusUpdate(previousState, currentState);
    }

    private void NetworkObjectStatusUpdate(NetworkObject networkObject, NetworkObjectStatus status)
    {
        if (!m_Camera)
        {
            return;
        }
        if (status == NetworkObjectStatus.Spawned)
        {
            m_Camera.transform.LookAt(networkObject.transform);
        }
    }
}
