using UnityEngine;
using Unity.Netcode.Components;

public class CollideTeleporter : MonoBehaviour
{
    public GameObject Destination;
    private void OnCollisionEnter(Collision collision)
    {
        var networkTransform = collision.gameObject.GetComponent<NetworkTransform>();
        if (networkTransform != null && Destination != null)
        {
            if (networkTransform.NetworkManager.IsServer)
            {
                var playerMover = collision.gameObject.GetComponent<PlayerMovement>();
                var position = Destination.transform.position;
                position.y = transform.position.y + 0.1f;
                networkTransform.Teleport(position, networkTransform.transform.rotation, networkTransform.transform.localScale);
                playerMover.Telporting();
            }
        }
    }
}
