using System.Collections.Generic;
using UnityEngine;

public class CameraViewExtension : BaseMonoExtension
{
    private static List<CameraViewExtension> s_Instances = new List<CameraViewExtension>();
    internal static void DestroyCameraInstances()
    {
        var instanceCount = s_Instances.Count;
        for (int i = instanceCount - 1; i >= 0; i--)
        {
            Destroy(s_Instances[i].gameObject);
        }
    }


    public bool DestroyOnLoad = true;
    protected Camera m_Camera;

    private Vector3 m_CameraOriginalPosition;
    private Quaternion m_CameraOriginalRotation;


    protected override void OnAwake()
    {
        if (!s_Instances.Contains(this))
        {
            s_Instances.Add(this);
        }
        base.OnAwake();
    }

    protected override void OnInitialize()
    {
        m_Camera = GetComponent<Camera>();
        m_CameraOriginalPosition = m_Camera.transform.position;
        m_CameraOriginalRotation = m_Camera.transform.rotation;
        if (!DestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
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
        if (!DestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
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
