using UnityEngine;
using Unity.Netcode;

public class ShipBulletBehavior : NetworkBehaviour
{
    public float BulletSpeed = 10f;

    public GameObject BulletOwner = null;

    private NetworkVariable<float> m_BulletLifetime = 
        new NetworkVariable<float>(NetworkVariableReadPermission.Everyone, 10f);

    // Update is called once per frame
    private void Update()
    {
        if (NetworkManager != null && NetworkManager.IsListening)
        {
            if (IsOwner)
            {
                transform.Translate(Vector2.right * BulletSpeed * Time.deltaTime);
            }

            if (IsServer)
            {
                m_BulletLifetime.Value -= Time.deltaTime;
                if (m_BulletLifetime.Value <= 0f)
                {
                    DespawnBullet();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only react to trigger on the server
        if (!IsServer)
        {
            return;
        }

        if (other.gameObject != BulletOwner)
        {
            var spacheshipController = other.gameObject.GetComponent<SpaceshipController>();
            if (spacheshipController != null)
            {
                DespawnBullet();

                spacheshipController.TakeDamage();
            }
        }
    }

    private void DespawnBullet()
    {
        gameObject.SetActive(false);

        // Server tells clients that this object is no longer in play
        NetworkObject.Despawn();
    }
}
