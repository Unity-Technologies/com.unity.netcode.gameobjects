using Unity.Netcode;
using UnityEngine;

public class ScalingCube : MonoBehaviour
{
    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        transform.localScale = new Vector3(Mathf.Repeat(Time.time * 2, 3f), transform.localScale.y, transform.localScale.z);
    }
}
