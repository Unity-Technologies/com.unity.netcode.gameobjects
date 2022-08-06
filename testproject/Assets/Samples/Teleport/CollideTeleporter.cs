using UnityEngine;
using Unity.Netcode.Components;
using Unity.Netcode;

public class CollideTeleporter : MonoBehaviour
{
    public GameObject Destination;
    public bool Preserve_XAxis;
    public bool Preserve_YAxis;
    public bool Preserve_ZAxis;

    private void OnCollisionEnter(Collision collision)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !NetworkManager.Singleton.IsServer)
        {
            return;
        }
        var playerMover = collision.gameObject.GetComponent<PlayerMovement>();
        if (playerMover == null || playerMover.IsTeleporting)
        {
            return;
        }

        var networkTransform = collision.gameObject.GetComponent<NetworkTransform>();
        if (networkTransform == null || Destination == null)
        {
            return;
        }
        var objectTransform = networkTransform.transform;
        var position = Destination.transform.position;

        if (Preserve_XAxis)
        {
            position.x = objectTransform.position.x;
        }

        if (Preserve_YAxis)
        {
            position.y = objectTransform.position.y;
        }
        else
        {
            position.y = transform.position.y + 0.1f;
        }

        if (Preserve_ZAxis)
        {
            position.z = objectTransform.position.z;
        }

        networkTransform.Teleport(position, networkTransform.transform.rotation, networkTransform.transform.localScale);
        playerMover.Telporting();
    }
}
