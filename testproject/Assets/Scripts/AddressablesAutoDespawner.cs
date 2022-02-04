using UnityEngine;

public class AddressablesAutoDespawner : MonoBehaviour
{
    [SerializeField] private float m_DelayToDespawn = 3.0f;

    // Start is called before the first frame update
    private void Start()
    {
       Invoke("DespawnAddressable", m_DelayToDespawn); 
    }

    private void DespawnAddressable()
    {
        // NOTE: (Cosmin) we are despawning this the normal way, not by calling any Addressable API
        // as the Addressable has been preloaded
        Destroy(gameObject);
    }

}
