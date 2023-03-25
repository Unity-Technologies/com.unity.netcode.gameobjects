using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class CollideTeleporter : MonoBehaviour
{
    [Tooltip("The destination GameObject transform. All rotation and scale will be applied, but position values can be ignored by setting the Preserve properties.")]
    public GameObject Destination;
    [Tooltip("When checked, the x-axis position value will remain the same during teleporting.")]
    public bool Preserve_XAxis;
    [Tooltip("When checked, the y-axis position value will remain the same during teleporting.")]
    public bool Preserve_YAxis;
    [Tooltip("When checked, the z-axis position value will remain the same during teleporting.")]
    public bool Preserve_ZAxis;

    private void OnCollisionEnter(Collision collision)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
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

        playerMover.Telporting(position);
    }
}
