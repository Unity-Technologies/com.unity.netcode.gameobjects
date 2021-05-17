using System;
using MLAPI.NetworkVariable;
using MLAPI.Transports;
using UnityEngine;
using UnityEngine.Serialization;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// A prototype component for syncing transforms
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkTransform")]
    public class NetworkTransform : NetworkBehaviour
    {
        /// <summary>
        /// Server authority only allows the server to update this transform
        /// Client authority only allows the client owner to update this transform
        /// Shared authority allows everyone to update this transform
        /// </summary>
        public enum Authority
        {
            Server = 0, // default
            Client,
            Shared
        }

        /// <summary>
        /// TODO this will need refactoring
        /// Specifies who can update this transform
        /// </summary>
        [SerializeField, Tooltip("Defines who can update this transform.")]
        public Authority TransformAuthority = Authority.Server; // todo Luke mentioned an incoming system to manage this at the NetworkBehaviour level, lets sync on this

        /// <summary>
        /// The base amount of sends per seconds to use when range is disabled
        /// </summary>
        [SerializeField, Range(0, 120), Tooltip("The base amount of sends per seconds to use when range is disabled")]
        public float FixedSendsPerSecond = 30f;

        /// <summary>
        /// TODO MTT-766 once we have per var interpolation
        /// Enable interpolation
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        [FormerlySerializedAs("m_InterpolatePosition")]
        [SerializeField, Tooltip("This requires AssumeSyncedSends to be true")]
        public bool InterpolatePosition = true;

        /// <summary>
        /// TODO MTT-766 once we have per var interpolation
        /// The distance before snaping to the position
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        [SerializeField, Tooltip("The transform will snap if the distance is greater than this distance")]
        public float SnapDistance = 10f;

        /// <summary>
        /// TODO MTT-766 once we have per var interpolation
        /// Should the server interpolate
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        [SerializeField]
        public bool InterpolateServer = true;

        /// <summary>
        /// TODO MTT-767 once we have this per var setting. The value check could be more on the network variable itself. If a server increases
        ///      a Netvar int by +0.05, the netvar would actually not transmit that info and would wait for the value to be even more different.
        ///      The setting in the NetworkTransform would be to just apply it to our netvars when available
        /// The min meters to move before a send is sent
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        [SerializeField, Tooltip("The min meters to move before a send is sent")]
        public float MinMeters = 0.15f;

        /// <summary>
        /// TODO MTT-767 once we have this per var setting
        /// The min degrees to rotate before a send is sent
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        [SerializeField, Tooltip("The min degrees to rotate before a send is sent")]
        public float MinDegrees = 1.5f;

        /// <summary>
        /// TODO MTT-767 once we have this per var setting
        /// The min meters to scale before a send is sent
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        [SerializeField, Tooltip("The min meters to scale before a send is sent")]
        public float MinSize = 0.15f;

        /// <summary>
        /// The channel to send the data on
        /// </summary>
        [SerializeField, Tooltip("The channel to send the data on.")]
        public NetworkChannel Channel = NetworkChannel.NetworkVariable;

        private Transform m_Transform;
        private NetworkVariableVector3 m_NetworkPosition = new NetworkVariableVector3();
        private NetworkVariableQuaternion m_NetworkRotation = new NetworkVariableQuaternion();
        private NetworkVariableVector3 m_NetworkWorldScale = new NetworkVariableVector3();
        // private NetworkTransform m_NetworkParent; // TODO handle this here?

        private Vector3 m_OldPosition;
        private Quaternion m_OldRotation;
        private Vector3 m_OldScale;

        private NetworkVariable<Vector3>.OnValueChangedDelegate m_PositionChangedDelegate;
        private NetworkVariable<Quaternion>.OnValueChangedDelegate m_RotationChangedDelegate;
        private NetworkVariable<Vector3>.OnValueChangedDelegate m_ScaleChangedDelegate;

        // todo really not happy with that one, hopefully we can have a cleaner solution with reparenting.
        private void SetWorldScale(Vector3 globalScale)
        {
            m_Transform.localScale = Vector3.one;
            var lossyScale = m_Transform.lossyScale;
            m_Transform.localScale = new Vector3(globalScale.x / lossyScale.x, globalScale.y / lossyScale.y, globalScale.z / lossyScale.z);
        }

        private bool CanUpdateTransform()
        {
            return (IsClient && TransformAuthority == Authority.Client && IsOwner) || (IsServer && TransformAuthority == Authority.Server) || TransformAuthority == Authority.Shared;
        }

        private void Awake()
        {
            m_Transform = transform;
        }

        public override void NetworkStart()
        {
            void SetupVar<T>(NetworkVariable<T> v, T initialValue, ref T oldVal)
            {
                v.Settings.SendTickrate = FixedSendsPerSecond;
                v.Settings.SendNetworkChannel = Channel;
                if (CanUpdateTransform())
                {
                    v.Value = initialValue;
                }
                oldVal = initialValue;
            }

            SetupVar(m_NetworkPosition, m_Transform.position, ref m_OldPosition);
            SetupVar(m_NetworkRotation, m_Transform.rotation, ref m_OldRotation);
            SetupVar(m_NetworkWorldScale, m_Transform.lossyScale, ref m_OldScale);

            if (TransformAuthority == Authority.Client)
            {
                m_NetworkPosition.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                m_NetworkRotation.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                m_NetworkWorldScale.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
            }
            else if (TransformAuthority == Authority.Shared)
            {
                m_NetworkPosition.Settings.WritePermission = NetworkVariablePermission.Everyone;
                m_NetworkRotation.Settings.WritePermission = NetworkVariablePermission.Everyone;
                m_NetworkWorldScale.Settings.WritePermission = NetworkVariablePermission.Everyone;
            }
        }

        private NetworkVariable<T>.OnValueChangedDelegate GetOnValueChangedDelegate<T>(Action<T> assignCurrent)
        {
            return (old, current) =>
            {
                if (TransformAuthority == Authority.Client && IsClient && IsOwner)
                {
                    // this should only happen for my own value changes.
                    // todo MTT-768 this shouldn't happen anymore with new tick system (tick written will be higher than tick read, so netvar wouldn't change in that case
                    return;
                }

                assignCurrent.Invoke(current);
            };
        }

        private void Start()
        {
            // Register on value changed delegate. We can't simply check the position every fixed update because of shared authority
            // Shared authority involves writing locally but applying changes when they come from the server. You can't both read from
            // your NetworkPosition and write to it in the same FixedUpdate, you need both separate.
            // There's no conflict resolution here. If two clients try to update the same value at the same time, they'll both think they are right
            m_PositionChangedDelegate = GetOnValueChangedDelegate<Vector3>(current =>
            {
                transform.position = current;
                m_OldPosition = current;
            });
            m_NetworkPosition.OnValueChanged += m_PositionChangedDelegate;
            m_RotationChangedDelegate = GetOnValueChangedDelegate<Quaternion>(current =>
            {
                transform.rotation = current;
                m_OldRotation = current;
            });
            m_NetworkRotation.OnValueChanged += m_RotationChangedDelegate;
            m_ScaleChangedDelegate = GetOnValueChangedDelegate<Vector3>(current =>
            {
                SetWorldScale(current);
                m_OldScale = current;
            });
            m_NetworkWorldScale.OnValueChanged += m_ScaleChangedDelegate;
        }

        public void OnDestroy()
        {
            m_NetworkPosition.OnValueChanged -= m_PositionChangedDelegate;
            m_NetworkRotation.OnValueChanged -= m_RotationChangedDelegate;
            m_NetworkWorldScale.OnValueChanged -= m_ScaleChangedDelegate;
        }

        private void FixedUpdate()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (CanUpdateTransform())
            {
                m_NetworkPosition.Value = m_Transform.position;
                m_NetworkRotation.Value = m_Transform.rotation;
                m_NetworkWorldScale.Value = m_Transform.lossyScale;
            }
            else if (m_Transform.position != m_OldPosition ||
                m_Transform.rotation != m_OldRotation ||
                m_Transform.lossyScale != m_OldScale
            )
            {
                Debug.LogError($"Trying to update transform's position for object { gameObject.name } with ID {NetworkObjectId} when you're not allowed, please validate your NetworkTransform's authority settings", gameObject);
                m_OldPosition = m_Transform.position;
                m_OldRotation = m_Transform.rotation;
                m_OldScale = m_Transform.lossyScale;
            }
        }

        /// <summary>
        /// Teleports the transform to the given values without interpolating
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="newRotation"></param>
        /// <param name="newScale"></param>
        public void Teleport(Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
        {
            throw new NotImplementedException(); // TODO MTT-769
        }
    }
}
