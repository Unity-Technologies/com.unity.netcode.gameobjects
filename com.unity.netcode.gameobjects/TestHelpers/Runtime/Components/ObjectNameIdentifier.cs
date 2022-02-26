
namespace Unity.Netcode.TestHelpers.Runtime
{
    public class ObjectNameIdentifier : NetworkBehaviour
    {
        private ulong m_CurrentOwner;
        private ulong m_CurrentNetworkObjectId;
        private bool m_IsRegistered;

        /// <summary>
        /// Keep a reference to the assigned NetworkObject
        /// <see cref="OnDestroy"/>
        /// </summary>
        private NetworkObject m_NetworkObject;

        public override void OnNetworkSpawn()
        {
            RegisterAndLabelNetworkObject();
        }

        protected void RegisterAndLabelNetworkObject()
        {
            if (!m_IsRegistered)
            {
                // This is required otherwise it will try to continue to update the NetworkBehaviour even if
                // it has been destroyed.
                m_NetworkObject = NetworkObject;
                m_CurrentOwner = OwnerClientId;
                m_CurrentNetworkObjectId = NetworkObjectId;
                var objectOriginalName = gameObject.name.Replace("(Clone)", "");
                var serverOrClient = IsServer ? "Server" : "Client";
                if (NetworkObject.IsPlayerObject)
                {
                    gameObject.name = NetworkManager.LocalClientId == OwnerClientId ? $"{objectOriginalName}({OwnerClientId})-Local{objectOriginalName}" :
                        $"{objectOriginalName}({OwnerClientId})-On{serverOrClient}({NetworkManager.LocalClientId})";
                }
                else
                {
                    gameObject.name = $"{objectOriginalName}({NetworkObjectId})-On{serverOrClient}({NetworkManager.LocalClientId})";
                }

                // Don't add the player objects to the global list of NetworkObjects
                if (!NetworkObject.IsPlayerObject)
                {
                    NetcodeIntegrationTest.RegisterNetworkObject(NetworkObject);
                }
                m_IsRegistered = true;
            }
        }

        protected void DeRegisterNetworkObject()
        {
            if (m_IsRegistered)
            {
                NetcodeIntegrationTest.DeregisterNetworkObject(m_CurrentOwner, m_CurrentNetworkObjectId);
                m_IsRegistered = false;
            }
        }

        public override void OnLostOwnership()
        {
            DeRegisterNetworkObject();
            RegisterAndLabelNetworkObject();
        }

        public override void OnGainedOwnership()
        {
            DeRegisterNetworkObject();
            RegisterAndLabelNetworkObject();
        }

        public override void OnNetworkDespawn()
        {
            DeRegisterNetworkObject();
        }

        public override void OnDestroy()
        {
            if (m_NetworkObject != null)
            {
                DeRegisterNetworkObject();
                // This is required otherwise it will try to continue to update the NetworkBehaviour even if
                // it has been destroyed (most likely integration test specific)
                if (m_NetworkObject.ChildNetworkBehaviours != null && m_NetworkObject.ChildNetworkBehaviours.Contains(this))
                {
                    NetworkObject.ChildNetworkBehaviours.Remove(this);
                }
                m_NetworkObject = null;
            }
            base.OnDestroy();
        }
    }
}
