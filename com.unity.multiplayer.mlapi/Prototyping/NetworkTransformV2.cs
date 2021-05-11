using System.Collections;
using System.Collections.Generic;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace MLAPI.Prototyping
{
    public class NetworkTransformV2 : NetworkBehaviour
    {
        public bool IsClientAuthoritative = false;
        public bool IsSharedObject = false;
        [Range(0, 120)]
        public float FixedSendsPerSecond = 20f;


        private NetworkVariableVector3 m_Position = new NetworkVariableVector3();
        private NetworkVariableQuaternion m_Rotation = new NetworkVariableQuaternion();
        private NetworkVariableVector3 m_Scale = new NetworkVariableVector3();
        private NetworkObject m_Parent;// = new NetworkObject(); // TODO handle this

        private NetworkTransformHandler m_Handler;

        private abstract class NetworkTransformHandler
        {
            protected NetworkTransformV2 m_NetworkTransform;
            public abstract void Awake();
            public abstract void FixedUpdate();

            public NetworkTransformHandler(NetworkTransformV2 networkTransform)
            {
                m_NetworkTransform = networkTransform;
            }
        }

        private class ClientNetworkTransformHandler : NetworkTransformHandler
        {
            public ClientNetworkTransformHandler(NetworkTransformV2 networkTransform) : base(networkTransform) { }

            public override void Awake()
            {
            }

            public override void FixedUpdate()
            {

            }
        }

        private class ServerNetworkTransformHandler : NetworkTransformHandler
        {
            public ServerNetworkTransformHandler(NetworkTransformV2 networkTransform) : base(networkTransform) { }

            public override void Awake()
            {
                if (m_NetworkTransform.IsClientAuthoritative && !m_NetworkTransform.IsSharedObject)
                {
                    m_NetworkTransform.m_Position.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                    m_NetworkTransform.m_Rotation.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                    m_NetworkTransform.m_Scale.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                }
                else if (m_NetworkTransform.IsClientAuthoritative && m_NetworkTransform.IsSharedObject)
                {
                    m_NetworkTransform.m_Position.Settings.WritePermission = NetworkVariablePermission.Everyone;
                    m_NetworkTransform.m_Rotation.Settings.WritePermission = NetworkVariablePermission.Everyone;
                    m_NetworkTransform.m_Scale.Settings.WritePermission = NetworkVariablePermission.Everyone;
                }


            }

            public override void FixedUpdate()
            {
            }
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

            m_Position.Settings.SendTickrate = FixedSendsPerSecond;
            m_Rotation.Settings.SendTickrate = FixedSendsPerSecond;
            m_Scale.Settings.SendTickrate = FixedSendsPerSecond;

            m_Handler.Awake();
        }

        private void FixedUpdate()
        {
            if ((IsClient && IsClientAuthoritative && IsOwner) || (IsServer && !IsClientAuthoritative))
            {
                m_Position.Value = transform.position;
                m_Rotation.Value = transform.rotation;
                m_Scale.Value = transform.localScale;
            }
            else
            {
                transform.position = m_Position.Value;
                transform.rotation = m_Rotation.Value;
                transform.localScale = m_Scale.Value;
            }

            m_Handler?.FixedUpdate();
        }
    }
}
