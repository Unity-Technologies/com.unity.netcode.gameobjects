using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

#if UNITY_EDITOR
using Unity.Netcode.Editor;
using UnityEditor;

/// <summary>
/// The custom editor for the <see cref="RotatingBodyLogic"/> component.
/// </summary>
[CustomEditor(typeof(RotatingBodyLogic), true)]
public class RotatingBodyLogicEditor : NetworkTransformEditor
{
    private SerializedProperty m_RotationSpeed;
    private SerializedProperty m_RotateDirection;
    private SerializedProperty m_ZAxisMove;
    private SerializedProperty m_PathMotion;


    public override void OnEnable()
    {
        m_RotationSpeed = serializedObject.FindProperty(nameof(RotatingBodyLogic.RotationSpeed));
        m_RotateDirection = serializedObject.FindProperty(nameof(RotatingBodyLogic.RotateDirection));
        m_ZAxisMove = serializedObject.FindProperty(nameof(RotatingBodyLogic.ZAxisMove));
        m_PathMotion = serializedObject.FindProperty(nameof(RotatingBodyLogic.PathMovement));
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField($"{nameof(RotatingBodyLogic)} Properties", EditorStyles.boldLabel);
        {
            EditorGUILayout.PropertyField(m_RotationSpeed);
            EditorGUILayout.PropertyField(m_RotateDirection);
            EditorGUILayout.PropertyField(m_ZAxisMove);
            EditorGUILayout.PropertyField(m_PathMotion);
        }
        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }
}
#endif

/// <summary>
/// Handles rotating the large in-scene placed platform/tunnels and parenting/deparenting players
/// </summary>
public class RotatingBodyLogic : NetworkTransform
{
    public enum RotationDirections
    {
        Clockwise,
        CounterClockwise
    }

    [Range(0.0f, 2.0f)]
    public float RotationSpeed = 1.0f;
    public RotationDirections RotateDirection;

    public List<GameObject> PathMovement;

    public bool ZAxisMove = false;


    private TagHandle m_TagHandle;
    private float m_RotationDirection;
    private float ZAxisMax;
    private float ZAxisDirection;
    private Vector3 OriginalForward;

    private int m_CurrentPathObject = -1;
    private GameObject m_CurrentNavPoint;

    protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
    {
        m_TagHandle = TagHandle.GetExistingTag("Player");
        m_RotationDirection = RotateDirection == RotationDirections.Clockwise ? 1.0f : -1.0f;
        ZAxisMax = transform.position.z;
        ZAxisDirection = Mathf.Sign(ZAxisMax) < 0 ? 1.0f : -1.0f;
        OriginalForward = transform.forward;
        m_NextSwitchDirection = Time.realtimeSinceStartup + 2.0f;
        SetNextPoint();
        base.OnNetworkPreSpawn(ref networkManager);
    }

    private void SetNextPoint()
    {
        if (PathMovement == null || PathMovement.Count == 0)
        {
            return;
        }
        m_CurrentPathObject++;
        m_CurrentPathObject %= PathMovement.Count;
        m_CurrentNavPoint = PathMovement[m_CurrentPathObject];
    }


    /// <summary>
    /// When triggered, the player is parented under the rotating body.
    /// </summary>
    /// <remarks>
    /// This is only triggered on the owner side since we disable the CharacterController
    /// on all non-owner instances.
    /// </remarks>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsSpawned || !other.CompareTag(m_TagHandle))
        {
            return;
        }
        var nonRigidPlayerMover = other.GetComponent<MoverScriptNoRigidbody>();
        if (nonRigidPlayerMover != null)
        {
            nonRigidPlayerMover.SetParent(NetworkObject);
        }
    }

    /// <summary>
    /// When triggered, the player is deparented from the rotating body.
    /// </summary>
    /// <remarks>
    /// This is only triggered on the owner side since we disable the CharacterController
    /// on all non-owner instances.
    /// </remarks>
    private void OnTriggerExit(Collider other)
    {
        if (!IsSpawned || !other.CompareTag(m_TagHandle))
        {
            return;
        }

        var nonRigidPlayerMover = other.GetComponent<MoverScriptNoRigidbody>();
        if (nonRigidPlayerMover != null)
        {
            nonRigidPlayerMover.SetParent(null);
        }
    }

    private float m_NextSwitchDirection;
    /// <summary>
    /// We rotate the body during late update to avoid fighting between the host/owner (depending upon network topology)
    /// motion and the body's motion/rotation.
    /// </summary>
    private void LateUpdate()
    {
        if (!IsSpawned || !CanCommitToTransform)
        {
            return;
        }

        if (m_CurrentNavPoint != null)
        {
            if (Vector3.Distance(m_CurrentNavPoint.transform.position, transform.position) <= 0.05f)
            {
                SetNextPoint();
            }

            var direction = (m_CurrentNavPoint.transform.position - transform.position).normalized;
            transform.position = Vector3.Lerp(transform.position, transform.position + direction * 10, Time.deltaTime);
        }
        else
        if (ZAxisMove)
        {
            if (Mathf.Abs(transform.position.z) > ZAxisMax && m_NextSwitchDirection < Time.realtimeSinceStartup)
            {
                ZAxisDirection *= -1;
                m_RotationDirection *= -1;
                m_NextSwitchDirection = Time.realtimeSinceStartup + 2.0f;
            }

            transform.position = Vector3.Lerp(transform.position, transform.position + (OriginalForward * ZAxisDirection * 10), Time.deltaTime);
        }

        if (RotationSpeed > 0.0f)
        {
            transform.right = Vector3.Lerp(transform.right, transform.forward * m_RotationDirection, Time.deltaTime * RotationSpeed);
        }
    }
}
