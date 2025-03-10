using UnityEngine;

public class CameraViewExtension : BaseMonoExtension
{
    public static CameraViewExtension Instance;
    private Camera m_Camera;

    private Vector3 m_CameraOriginalPosition;
    private Quaternion m_CameraOriginalRotation;

    protected override void OnInitialize()
    {
        m_Camera = GetComponent<Camera>();
        m_CameraOriginalPosition = m_Camera.transform.position;
        m_CameraOriginalRotation = m_Camera.transform.rotation;
        if (Instance && Instance.gameObject && Instance.gameObject != gameObject)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        base.OnInitialize();
    }

    public void ParentCamera(Transform transform, bool worldPositionStays = false)
    {
        ResetCamera();
        m_Camera.transform.SetParent(transform, worldPositionStays);
    }

    public bool IsParentedUnder(Transform transform)
    {
        if (m_Camera && m_Camera.transform.parent)
        {
            return m_Camera.transform.parent == transform;
        }
        return false;
    }

    public void ResetCamera()
    {
        if (!m_Camera)
        {
            return;
        }
        m_Camera.transform.parent = null;
        m_Camera.transform.position = m_CameraOriginalPosition;
        m_Camera.transform.rotation = m_CameraOriginalRotation;
        DontDestroyOnLoad(gameObject);
    }

    protected override void OnStatusUpdate(ConnectionStates previousState, ConnectionStates currentState)
    {
        if (previousState == ConnectionStates.Connected && currentState == ConnectionStates.None)
        {
            if (m_Camera && m_Camera.transform.parent)
            {
                ResetCamera();
            }
        }
        base.OnStatusUpdate(previousState, currentState);
    }
}
