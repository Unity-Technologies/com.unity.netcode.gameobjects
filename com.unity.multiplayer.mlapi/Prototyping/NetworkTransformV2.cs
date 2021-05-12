using System;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace MLAPI.Prototyping
{
    public class NetworkTransformV2 : NetworkBehaviour
    {
        public bool IsClientAuthoritative;
        public bool IsSharedObject;
        [Range(0, 120)]
        public float FixedSendsPerSecond = 20f;

        private Transform m_Transform;
        private NetworkVariableVector3 m_NetworkPosition = new NetworkVariableVector3(); // TODO use netvar interpolation when available
        private NetworkVariableQuaternion m_NetworkRotation = new NetworkVariableQuaternion();
        private NetworkVariableVector3 m_NetworkScale = new NetworkVariableVector3();
        private Vector3 m_OldPosition;
        private Quaternion m_OldRotation;
        private Vector3 m_OldScale;
        private NetworkObject m_Parent;// = new NetworkObject(); // TODO handle this here?

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
                if (m_NetworkTransform.IsClientAuthoritative && !m_NetworkTransform.IsSharedObject)
                {
                    m_NetworkTransform.m_NetworkPosition.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                    m_NetworkTransform.m_NetworkRotation.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                    m_NetworkTransform.m_NetworkScale.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                }
                else if (m_NetworkTransform.IsClientAuthoritative && m_NetworkTransform.IsSharedObject)
                {
                    m_NetworkTransform.m_NetworkPosition.Settings.WritePermission = NetworkVariablePermission.Everyone;
                    m_NetworkTransform.m_NetworkRotation.Settings.WritePermission = NetworkVariablePermission.Everyone;
                    m_NetworkTransform.m_NetworkScale.Settings.WritePermission = NetworkVariablePermission.Everyone;
                }


            }

            public override void FixedUpdate()
            {
            }
        }

        private void Awake()
        {
            m_Transform = transform;
            m_OldPosition = m_Transform.position;
            m_OldRotation = m_Transform.rotation;
            m_OldScale = m_Transform.localScale;
        }

        public override void NetworkStart()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                m_Handler = new ClientNetworkTransformHandler(this);
            }
            else
            {
                m_Handler = new ServerNetworkTransformHandler(this);
            }

            m_NetworkPosition.Settings.SendTickrate = FixedSendsPerSecond;
            m_NetworkRotation.Settings.SendTickrate = FixedSendsPerSecond;
            m_NetworkScale.Settings.SendTickrate = FixedSendsPerSecond;

            if (CanUpdateTransform() || IsSharedObject)
            {
                m_NetworkPosition.Value = m_Transform.position;
                m_NetworkRotation.Value = m_Transform.rotation;
                m_NetworkScale.Value = m_Transform.localScale;
            }

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
                transform.localScale = current;
                m_OldScale = current;
            });
            m_NetworkScale.OnValueChanged += m_ScaleChangedDelegate;

            m_Handler.NetworkStart();
        }

        public void OnDestroy()
        {
            m_NetworkPosition.OnValueChanged -= m_PositionChangedDelegate;
            m_NetworkRotation.OnValueChanged -= m_RotationChangedDelegate;
            m_NetworkScale.OnValueChanged -= m_ScaleChangedDelegate;
        }

        private bool CanUpdateTransform()
        {
            return (IsClient && IsClientAuthoritative && IsOwner) || (IsServer && !IsClientAuthoritative);
        }

        private NetworkVariable<T>.OnValueChangedDelegate GetOnValueChanged<T>(Action<T> assignCurrent)
        {
            return (old, current) =>
            {
                var isClientOnly = !IsServer;
                if (IsClientAuthoritative && isClientOnly && !IsSharedObject && IsOwner)
                {
                    // this should only happen for my own value changes.
                    return;
                }

                assignCurrent.Invoke(current);
            };
        }

        // private void OnPositionChanged(Vector3 old, Vector3 current)
        // {
        //     var isClientOnly = !IsServer;
        //     if (IsClientAuthoritative && isClientOnly && !IsSharedObject && IsOwner)
        //     {
        //         // this should only happen for my own position changes.
        //         return;
        //     }
        //     transform.position = current;
        //     m_OldPosition = current;
        // }

        private void FixedUpdate()
        {
            if (NetworkManager == null || (!NetworkManager.IsServer && !NetworkManager.IsClient))
            {
                return;
            }

            // saving current network value so we don't override changes coming from the network with changes done locally
            if (CanUpdateTransform() || IsSharedObject)
            {
                m_NetworkPosition.Value = m_Transform.position;
                m_NetworkRotation.Value = m_Transform.rotation;
                m_NetworkScale.Value = m_Transform.localScale;
            }
            else if (m_Transform.position != m_OldPosition ||
                m_Transform.rotation != m_OldRotation ||
                m_Transform.localScale != m_OldScale
            )
            {
                Debug.LogError("Trying to update transform's position when you're not allowed, please validate your NetworkTransform's authority settings");
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
