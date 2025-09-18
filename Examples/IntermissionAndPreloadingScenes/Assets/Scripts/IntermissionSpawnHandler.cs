using Unity.Netcode;
using UnityEngine.SceneManagement;

public class IntermissionSpawnHandler : NetworkBehaviour
{
    private SceneIntermission m_SceneIntermission;

    private void Awake()
    {
        m_SceneIntermission = FindFirstObjectByType<SceneIntermission>();
    }

    protected override void OnNetworkPostSpawn()
    {
        if (HasAuthority && m_SceneIntermission)
        {
            m_SceneIntermission.OnIntermissionActiveUpdate += OnIntermissionActiveUpdate;
            DontDestroyOnLoad(gameObject);
        }
        base.OnNetworkPostSpawn();
    }

    private void OnIntermissionActiveUpdate(bool isActive)
    {
        if (!isActive)
        {
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
