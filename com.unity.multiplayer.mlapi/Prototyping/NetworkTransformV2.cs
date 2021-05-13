using System;
using MLAPI.NetworkVariable;
using MLAPI.Transports;
using UnityEngine;
using UnityEngine.Serialization;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// A prototype component for syncing transforms
    /// TODO remove the V2 after review
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkTransform")]
    public class NetworkTransformV2 : NetworkBehaviour
    {
        public enum Authority
        {
            Server = 0, // default
            Client,
            Shared
        }
        [SerializeField, Tooltip("Defines who can update this transform.")]
        private Authority m_Authority; // todo Luke mentioned an incoming system to manage this at the NetworkBehaviour level, lets sync on this

        /// <summary>
        /// The base amount of sends per seconds to use when range is disabled
        /// </summary>
        [Range(0, 120)]
        public float FixedSendsPerSecond = 30f;

        /// <summary>
        /// TODO once we have per var interpolation
        /// Enable interpolation
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        [Tooltip("This requires AssumeSyncedSends to be true")]
        public bool InterpolatePosition = true;

        /// <summary>
        /// TODO once we have per var interpolation
        /// The distance before snaping to the position
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        [Tooltip("The transform will snap if the distance is greater than this distance")]
        public float SnapDistance = 10f;

        /// <summary>
        /// TODO once we have per var interpolation
        /// Should the server interpolate
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        public bool InterpolateServer = true;

        /// <summary>
        /// TODO once we have this per var setting
        /// The min meters to move before a send is sent
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        public float MinMeters = 0.15f;

        /// <summary>
        /// TODO once we have this per var setting
        /// The min degrees to rotate before a send it sent
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        public float MinDegrees = 1.5f;

        /// <summary>
        /// TODO once we have this per var setting
        /// The min meters to scale before a send it sent
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        public float MinSize = 0.15f;

        /// <summary>
        /// The channel to send the data on
        /// </summary>
        [Tooltip("The channel to send the data on.")]
        public NetworkChannel Channel = NetworkChannel.NetworkVariable;

        // // commenting this out since we already have authority validation. Since client driven net var changes
        // // are authoritative, there's no use validating the net var change server side.
        // // Leaving this here for review, but this should disappear in upcoming revision. TODO
        // /// <summary>
        // /// The delegate used to check if a move is valid
        // /// </summary>
        // /// <param name="clientId">The client id the move is being validated for</param>
        // /// <param name="oldPos">The previous position</param>
        // /// <param name="newPos">The new requested position</param>
        // /// <returns>Returns Whether or not the move is valid</returns>
        // public delegate bool ValueChangeValidationDelegate<T>(ulong clientId, T oldValue, T newValue);
        //
        // /// <summary>
        // /// If set, moves will only be accepted if the custom delegate returns true
        // /// </summary>
        // public ValueChangeValidationDelegate<Vector3> IsMoveValidDelegate = null;

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

        private void SetWorldScale(Vector3 globalScale)
        {
            m_Transform.localScale = Vector3.one;
            var lossyScale = m_Transform.lossyScale;
            m_Transform.localScale = new Vector3(globalScale.x / lossyScale.x, globalScale.y / lossyScale.y, globalScale.z / lossyScale.z);
        }

        private void Awake()
        {
            m_Transform = transform;
        }

        private bool m_NetworkStarted;

        public override void NetworkStart()
        {
            m_NetworkStarted = true;
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

            // m_OldPosition = m_Transform.position;
            // m_OldRotation = m_Transform.rotation;
            // m_OldScale = m_Transform.lossyScale;

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

            if (IsServer)
            {
                if (m_Authority == Authority.Client)
                {
                    m_NetworkPosition.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                    m_NetworkRotation.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                    m_NetworkWorldScale.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                }
                else if (m_Authority == Authority.Shared)
                {
                    m_NetworkPosition.Settings.WritePermission = NetworkVariablePermission.Everyone;
                    m_NetworkRotation.Settings.WritePermission = NetworkVariablePermission.Everyone;
                    m_NetworkWorldScale.Settings.WritePermission = NetworkVariablePermission.Everyone;
                }
            }
        }

        public void OnDestroy()
        {
            m_NetworkPosition.OnValueChanged -= m_PositionChangedDelegate;
            m_NetworkRotation.OnValueChanged -= m_RotationChangedDelegate;
            m_NetworkWorldScale.OnValueChanged -= m_ScaleChangedDelegate;
        }

        private bool CanUpdateTransform()
        {
            return (IsClient && m_Authority == Authority.Client && IsOwner) || (IsServer && m_Authority == Authority.Server) || m_Authority == Authority.Shared;
        }

        private NetworkVariable<T>.OnValueChangedDelegate GetOnValueChangedDelegate<T>(Action<T> assignCurrent)
        {
            return (old, current) =>
            {
                if (m_Authority == Authority.Client && IsClient && IsOwner)
                {
                    // this should only happen for my own value changes.
                    // todo this shouldn't happen anymore with new tick system (tick written will be higher than tick read, so netvar wouldn't change in that case
                    return;
                }

                assignCurrent.Invoke(current);
            };
        }

        private void FixedUpdate()
        {
            // if (NetworkManager == null || (!NetworkManager.IsServer && !NetworkManager.IsClient))
            if (!m_NetworkStarted)
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
            // TODO
            throw new NotImplementedException();
        }
    }
}
