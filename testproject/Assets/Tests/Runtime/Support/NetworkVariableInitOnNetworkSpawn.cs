using Unity.Netcode;
using NUnit.Framework;

namespace TestProject.RuntimeTests.Support
{
    public class NetworkVariableInitOnNetworkSpawn : NetworkBehaviour
    {
        private NetworkVariable<int> m_Variable = new NetworkVariable<int>();
        public static bool NetworkSpawnCalledOnServer;
        public static bool NetworkSpawnCalledOnClient;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                NetworkSpawnCalledOnServer = true;
                m_Variable.Value = 5;
            }
            else
            {
                NetworkSpawnCalledOnClient = true;
            }
            Assert.AreEqual(5, m_Variable.Value);
        }
    }
}
