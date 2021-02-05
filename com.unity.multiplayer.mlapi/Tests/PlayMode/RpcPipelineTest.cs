using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
namespace MLAPI.Tests
{
    public interface IMLAPIUnitTestObject
    {
        public bool IsTestComplete();
    }

    public class RpcPipelineTest : NetworkedBehaviour,IMLAPIUnitTestObject
    {
        public bool pingSelfEnabled;

        [Range(1,100)]
        public int maxTestPingCount = 10;
        // Start is called before the first frame update
        void Start()
        {
            m_Serverparms.Send.UpdateStage = NetworkUpdateManager.NetworkUpdateStage.FixedUpdate;
            m_Clientparms.Send.UpdateStage = NetworkUpdateManager.NetworkUpdateStage.Default;
        }

        public bool IsTestComplete()
        {
            if(m_Counter >= maxTestPingCount)
            {
                return true;
            }
            return false;
        }

        private int m_Counter = 0;
        private float m_NextUpdate = 0.0f;
        private ServerRpcParams m_Serverparms;
        private ClientRpcParams m_Clientparms;
        private NetworkUpdateManager.NetworkUpdateStage m_LastUpdateStage;

        // Update is called once per frame
        void Update()
        {
            if(NetworkingManager.Singleton.IsListening && pingSelfEnabled && m_NextUpdate < Time.realtimeSinceStartup)
            {
                if(NetworkingManager.Singleton.IsListening && pingSelfEnabled && m_NextUpdate < Time.realtimeSinceStartup)
                {
                    m_NextUpdate = Time.realtimeSinceStartup + 0.5f;
                    m_LastUpdateStage = m_Serverparms.Send.UpdateStage;
                    PingMySelfServerRPC(m_Counter,m_Serverparms);
                    m_Clientparms.Send.UpdateStage = m_Serverparms.Send.UpdateStage;
                    switch(m_Serverparms.Send.UpdateStage)
                    {
                        case NetworkUpdateManager.NetworkUpdateStage.FixedUpdate:
                        {
                            m_Serverparms.Send.UpdateStage = NetworkUpdateManager.NetworkUpdateStage.Update;
                            break;
                        }
                        case NetworkUpdateManager.NetworkUpdateStage.Update:
                        {
                            m_Serverparms.Send.UpdateStage = NetworkUpdateManager.NetworkUpdateStage.LateUpdate;
                            break;
                        }
                        case NetworkUpdateManager.NetworkUpdateStage.LateUpdate:
                        {
                            m_Serverparms.Send.UpdateStage = NetworkUpdateManager.NetworkUpdateStage.FixedUpdate;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// PingMySelfServerRPC
        /// </summary>
        /// <param name="pingnumber">current number of pings</param>
        /// <param name="parameters">server rpc parameters</param>
        [ServerRpc]
        void PingMySelfServerRPC(int pingnumber, ServerRpcParams parameters)
        {
            Debug.Log("[HostClient][ServerRpc] Ping Number [" + pingnumber.ToString() + "] executed during the " + m_LastUpdateStage.ToString() + " stage. (previous output line should confirm this)");
            PingMySelfClientRpc(m_Counter,m_Clientparms);
            m_Counter++;
        }

        /// <summary>
        /// PingMySelfClientRpc
        /// Called by PingMySelfServerRPC to validate both Client->Server and Server-Client pipeline is working
        /// </summary>
        /// <param name="pingnumber">current number of pings</param>
        /// <param name="parameters">client rpc parameters</param>
        [ClientRpc]
        void PingMySelfClientRpc(int pingnumber, ClientRpcParams parameters)
        {
            Debug.Log("[HostServer][ClientRpc] Ping Back Number [" + pingnumber.ToString() + "] executed during the " + m_LastUpdateStage.ToString() + " stage. (previous output line should confirm this)");
        }
    }
}
