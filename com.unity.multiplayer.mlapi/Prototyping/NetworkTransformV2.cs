using System;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// TODO remove the V2 after review
    /// </summary>
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
        [Range(0, 120)]
        public float FixedSendsPerSecond = 30f; // todo have a global config for this? As a user, I wouldn't want to have to update my 1k objects if I realize it's not high enough late in the project

        private Transform m_Transform;
        private NetworkVariableVector3 m_NetworkPosition = new NetworkVariableVector3(); // TODO use netvar interpolation when available
        private NetworkVariableQuaternion m_NetworkRotation = new NetworkVariableQuaternion();
        private NetworkVariableVector3 m_NetworkWorldScale = new NetworkVariableVector3();
        // private NetworkTransform m_NetworkParent; // TODO handle this here? Needs to reparent NetworkObject, since current protocol uses NetworkObject+NetworkBehaviour+NetworkVariable hierarchy

        private Vector3 m_OldPosition;
        private Quaternion m_OldRotation;
        private Vector3 m_OldScale;

        private NetworkTransformHandler m_Handler;
        private NetworkVariable<Vector3>.OnValueChangedDelegate m_PositionChangedDelegate;
        private NetworkVariable<Quaternion>.OnValueChangedDelegate m_RotationChangedDelegate;
        private NetworkVariable<Vector3>.OnValueChangedDelegate m_ScaleChangedDelegate;

        private abstract class NetworkTransformHandler
        {
            protected NetworkTransformV2 m_NetworkTransform;
            public abstract void NetworkStart();
            public abstract void FixedUpdate();

            public NetworkTransformHandler(NetworkTransformV2 networkTransform)
            {
                m_NetworkTransform = networkTransform;
            }
        }

        private class ClientNetworkTransformHandler : NetworkTransformHandler
        {
            public ClientNetworkTransformHandler(NetworkTransformV2 networkTransform) : base(networkTransform) { }

            public override void NetworkStart()
            {
            }

            public override void FixedUpdate()
            {

            }
        }

        private class ServerNetworkTransformHandler : NetworkTransformHandler
        {
            public ServerNetworkTransformHandler(NetworkTransformV2 networkTransform) : base(networkTransform) { }

            public override void NetworkStart()
            {
                if (m_NetworkTransform.m_Authority == Authority.Client)
                {
                    m_NetworkTransform.m_NetworkPosition.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                    m_NetworkTransform.m_NetworkRotation.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                    m_NetworkTransform.m_NetworkWorldScale.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                }
                else if (m_NetworkTransform.m_Authority == Authority.Shared)
                {
                    m_NetworkTransform.m_NetworkPosition.Settings.WritePermission = NetworkVariablePermission.Everyone;
                    m_NetworkTransform.m_NetworkRotation.Settings.WritePermission = NetworkVariablePermission.Everyone;
                    m_NetworkTransform.m_NetworkWorldScale.Settings.WritePermission = NetworkVariablePermission.Everyone;
                }
            }

            public override void FixedUpdate()
            {
            }
        }

        private void SetWorldScale (Vector3 globalScale)
        {
            m_Transform.localScale = Vector3.one;
            var lossyScale = m_Transform.lossyScale;
            m_Transform.localScale = new Vector3 (globalScale.x/lossyScale.x, globalScale.y/lossyScale.y, globalScale.z/lossyScale.z);
        }

        private void Awake()
        {
            m_Transform = transform;
        }

        private bool networkStarted;

        public override void NetworkStart()
        {
            networkStarted = true;
            void SetupVar<T>(NetworkVariable<T> v, T initialValue, ref T oldVal)
            {
                v.Settings.SendTickrate = FixedSendsPerSecond;
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

            m_PositionChangedDelegate = GetOnValueChanged<Vector3>(current =>
            {
                transform.position = current;
                m_OldPosition = current;
            });
            m_NetworkPosition.OnValueChanged += m_PositionChangedDelegate;
            m_RotationChangedDelegate = GetOnValueChanged<Quaternion>(current =>
            {
                transform.rotation = current;
                m_OldRotation = current;
            });
            m_NetworkRotation.OnValueChanged += m_RotationChangedDelegate;
            m_ScaleChangedDelegate = GetOnValueChanged<Vector3>(current =>
            {
                SetWorldScale(current);
                m_OldScale = current;
            });
            m_NetworkWorldScale.OnValueChanged += m_ScaleChangedDelegate;

            if (!NetworkManager.Singleton.IsServer)
            {
                m_Handler = new ClientNetworkTransformHandler(this);
            }
            else
            {
                m_Handler = new ServerNetworkTransformHandler(this);
            }
            m_Handler.NetworkStart();
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

        private NetworkVariable<T>.OnValueChangedDelegate GetOnValueChanged<T>(Action<T> assignCurrent)
        {
            return (old, current) =>
            {
                var isClientOnly = !IsServer;
                // if (m_Authority == Authority.Client && isClientOnly && IsOwner)
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
            if (!networkStarted)
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

            m_Handler?.FixedUpdate();
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
