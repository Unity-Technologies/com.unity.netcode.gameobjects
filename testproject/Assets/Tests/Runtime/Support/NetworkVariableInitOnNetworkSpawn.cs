using Unity.Netcode;

namespace TestProject.RuntimeTests.Support
{
    public class NetworkVariableInitOnNetworkSpawn : NetworkBehaviour
    {
        public NetworkVariable<int> Variable = new NetworkVariable<int>();
        public static bool NetworkSpawnCalledOnServer;
        public static bool NetworkSpawnCalledOnClient;
        public static bool OnValueChangedCalledOnClient = false;

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
            Assert.IsFalse(OnValueChangedCalledOnClient);
            base.OnNetworkSpawn();
            if (IsServer)
            {
                NetworkSpawnCalledOnServer = true;
            }
            else
            {
                NetworkSpawnCalledOnClient = true;
            }
            Assert.AreEqual(5, Variable.Value);
        }
    }
}
