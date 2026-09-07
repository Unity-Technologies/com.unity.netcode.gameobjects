using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using Unity.Netcode.Editor;
using UnityEditor;

// This bypases the default custom editor for NetworkTransform
// and lets you modify your custom NetworkTransform's properties
// within the inspector view
[CustomEditor(typeof(PlayerMotion), true)]
public class PlayerMotionHandlerEditor : NetworkTransformEditor
{
    private SerializedProperty m_Radius;
    private SerializedProperty m_Speed;

    public override void OnEnable()
    {
        m_Radius = serializedObject.FindProperty(nameof(PlayerMotion.Radius));
        m_Speed = serializedObject.FindProperty(nameof(PlayerMotion.Speed));
        base.OnEnable();
    }

    private void DisplayPlayerMotionHandlerProperties()
    {
        EditorGUILayout.PropertyField(m_Radius);
        EditorGUILayout.PropertyField(m_Speed);
    }

    public override void OnInspectorGUI()
    {
        var playerMotion = target as PlayerMotion;
        void SetExpanded(bool expanded) { playerMotion.PlayerMotionExpanded = expanded; };
        DrawFoldOutGroup<PlayerMotion>(playerMotion.GetType(), DisplayPlayerMotionHandlerProperties, playerMotion.PlayerMotionExpanded, SetExpanded);
        base.OnInspectorGUI();
    }
}
#endif

/// <summary>
///  Just moves the player around automatically in a circular motion
/// </summary>
public class PlayerMotion : NetworkTransform
{
#if UNITY_EDITOR
    public bool PlayerMotionExpanded;
#endif

    [Range(1.0f, 20.0f)]
    public float Radius = 10.0f;

    [Range(1.0f, 30.0f)]
    public float Speed = 5.0f;

    private float m_CurrentPi;
    private float m_Increment = 0.25f;
    private float m_ClockWise = 1.0f;
    private Rigidbody m_RigidBody;
    private ComponentController m_ComponentController;

    protected override void Awake()
    {
        base.Awake();
        m_ComponentController = GetComponent<ComponentController>();
        m_RigidBody = GetComponent<Rigidbody>();
        m_RigidBody.useGravity = false;
    }

    public override void OnNetworkSpawn()
    {
        // Always invoked base when deriving from NetworkTransform
        base.OnNetworkSpawn();
        name = $"{name}-{OwnerClientId}";

        if (CanCommitToTransform)
        {
            m_RigidBody.useGravity = true;
            m_CurrentPi = Random.Range(-Mathf.PI, Mathf.PI);
            m_ClockWise = Random.Range(-1.0f, 1.0f);
            m_ClockWise = m_ClockWise / Mathf.Abs(m_ClockWise);
            if (!IsOwner)
            {
                Radius += Random.Range(-2.0f, 2.0f);
            }
        }
    }

    protected override void OnNetworkPostSpawn()
    {
        base.OnNetworkPostSpawn();
        // The default is for the authority to handle ComponentController changes
        if (HasAuthority)
        {
            // You can derive from ComponentController and override the OnHasAuthority virtual method
            // to make the Owner the authority as well.
            m_ComponentController.SetEnabled(true);
        }
    }

    public override void OnNetworkPreDespawn()
    {
        m_RigidBody.useGravity = false;
        if (HasAuthority)
        {            
            m_ComponentController.SetEnabled(true);
        }
        base.OnNetworkPreDespawn();
    }

    private void FixedUpdate()
    {
        if (IsSpawned && CanCommitToTransform)
        {
            // When loading a scene, don't move around and disable gravity
            m_RigidBody.useGravity = !ExtendedNetworkManager.Instance.IsSceneEventInProgress();
            if (ExtendedNetworkManager.Instance.IsSceneEventInProgress())
            {
                return;
            }
            m_CurrentPi += m_ClockWise * (Speed * m_Increment * Time.fixedDeltaTime);
            var position = transform.position;
            var offset = new Vector3(Radius * Mathf.Cos(m_CurrentPi), position.y, Radius * Mathf.Sin(m_CurrentPi));
            m_RigidBody.MovePosition(Vector3.Lerp(position, offset, Speed * 0.1f * Time.fixedDeltaTime));
        }
    }
}


