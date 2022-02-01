using Unity.Netcode;
using NUnit.Framework;

namespace TestProject.RuntimeTests.Support
{
    public class NetworkVariableInitOnNetworkSpawn : NetworkBehaviour
    {
        public NetworkVariable<int> Variable = new NetworkVariable<int>();
        public static bool NetworkSpawnCalledOnServer;
        public static bool NetworkSpawnCalledOnClient;
        public static bool OnValueChangedCalledOnClient;
        public static int ExpectedSpawnValueOnClient;

        private void Awake()
        {
            Variable.OnValueChanged += OnValueChanged;
        }

        public void OnValueChanged(int previousValue, int newValue)
        {
            if (!IsServer)
            {
                OnValueChangedCalledOnClient = true;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                NetworkSpawnCalledOnServer = true;
                Variable.Value = 5;
            }
            else
            {
                NetworkSpawnCalledOnClient = true;
                Assert.AreEqual(ExpectedSpawnValueOnClient, Variable.Value);
            }
        }
    }
}
