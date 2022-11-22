using UnityEngine;
using Unity.Netcode;

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
#pragma warning disable 612, 618
#endif
        var networkManagerInstances = FindObjectsOfType<NetworkManager>();
#if UNITY_2023_1_OR_NEWER
#pragma warning restore 612, 618
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
