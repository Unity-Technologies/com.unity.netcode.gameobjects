using UnityEngine;

/// <summary>
/// Serves as access point from code to a prefab
/// </summary>
public class PrefabReference : MonoBehaviour
{
    [SerializeField]
    public GameObject referencedPrefab;

    public static PrefabReference Instance { get; private set; }

    public void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
