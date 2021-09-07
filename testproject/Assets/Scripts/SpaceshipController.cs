using UnityEngine;
using Unity.Netcode;

public class SpaceshipController : NetworkBehaviour
{
    private NetworkVariable<int> m_ShipHealth = new NetworkVariable<int>(NetworkVariableReadPermission.Everyone, 3);

    [SerializeField]
    private float m_MovementSpeed;

    [SerializeField]
    private GameObject m_BulletPrefab;

    private Vector2 m_Direction = new Vector2();

    private Rigidbody2D m_Rigidbody2D;

    // Similar to Start(), but for the Networking session
    public override void OnNetworkSpawn()
    {
        // your additional code here...

        m_Rigidbody2D = gameObject.GetComponent<Rigidbody2D>();

        base.OnNetworkSpawn();
    }

    private void Update()
    {
        if (IsOwner)
        {
            m_Direction = Vector2.zero;

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                m_Direction.x -= 1f;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                m_Direction.x += 1f;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                m_Direction.y -= 1f;
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                m_Direction.y += 1f;
            }

            m_Direction.Normalize();

            transform.Translate(m_Direction * m_MovementSpeed * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Send a request to the server to spawn a bullet
                ShootBulletServerRPC();
            }
        }
    }

    [ServerRpc]
    private void ShootBulletServerRPC()
    {
        if (m_BulletPrefab == null)
        {
            Debug.Log("ERROR: Cannot shoot, bullet prefab is null!");
            return;
        }

        var newBullet = Instantiate(m_BulletPrefab, transform.position, Quaternion.identity);
        newBullet.transform.Rotate(Vector3.up, transform.eulerAngles.y);

        var shipBulletBehavior = newBullet.GetComponent<ShipBulletBehavior>();
        shipBulletBehavior.bulletOwner = gameObject;

        newBullet.GetComponent<NetworkObject>().Spawn();
    }

    [ClientRpc]
    private void ShipIsDeadClientRPC()
    {
        if (IsOwner)
        {
            m_MovementSpeed = 0f;
        }
    }

    public void TakeDamage()
    {
        if (IsServer)
        {
            m_ShipHealth.Value -= 1;

            if (m_ShipHealth.Value <= 0)
            {
                // update the client, tell it that it's spaceship is defeated
                ShipIsDeadClientRPC();
            }
        }
    }
}
