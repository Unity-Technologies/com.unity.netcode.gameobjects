using Unity.Netcode;
using UnityEngine;

/// <summary>
/// This can be added to the same GameObject the NetworkManager component is assigned to in order to prevent
/// multiple NetworkManager instances from being instantiated if the same scene is loaded.
/// </summary>
public class NetworkManagerMonitor : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
#if UNITY_2023_1_OR_NEWER
        var networkManagerInstances = FindObjectsByType<NetworkManager>(FindObjectsSortMode.InstanceID);
#else
        var networkManagerInstances = FindObjectsOfType<NetworkManager>();
#endif
        foreach (var instance in networkManagerInstances)
        {
            if (instance.IsListening)
            {
                if (gameObject != instance.gameObject)
                {
                    var networkManager = GetComponent<NetworkManager>();
                    Destroy(gameObject);
                }
            }
        }
    }
}
