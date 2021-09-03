using UnityEngine;
using Unity.Netcode;

public class ParentingSpawnHandler : NetworkBehaviour
{
    public GameObject PrefabToSpawnAndParent;

    [SerializeField]
    private float m_MoverVelocity;

    public override void OnNetworkSpawn()
    {
        if (IsServer && PrefabToSpawnAndParent != null)
        {
            var objectToParent = Instantiate(PrefabToSpawnAndParent);
            objectToParent.transform.position = transform.position;
            objectToParent.transform.rotation = transform.rotation;
            objectToParent.transform.localScale = transform.localScale;
            var gameObject = objectToParent.GetComponent<NetworkObject>();
            gameObject.Spawn(false);
            float ang = Random.Range(0.0f, 2 * Mathf.PI);
            gameObject.GetComponent<GenericMover>().SetDirectionAndVelocity(new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)), m_MoverVelocity);
        }
        base.OnNetworkSpawn();
    }


}
