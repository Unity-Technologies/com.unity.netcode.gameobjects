using UnityEngine;
using Unity.Netcode;

public class ShipBulletBehavior : NetworkBehaviour
{
    public float bulletSpeed = 10f;

    public GameObject bulletOwner = null;

    private NetworkVariable<float> bulletLifetime = 
        new NetworkVariable<float>(NetworkVariableReadPermission.Everyone, 10f);

    // Update is called once per frame
    void Update()
    {
        if (IsOwner)
        {
            transform.Translate(Vector2.right * bulletSpeed * Time.deltaTime);
        }

        if (IsServer)
        {
            bulletLifetime.Value -= Time.deltaTime;
            if (bulletLifetime.Value <= 0f)
            {
                DespawnBullet();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only react to trigger on the server
        if (!IsServer)
            return;

        if (other.gameObject != bulletOwner)
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
