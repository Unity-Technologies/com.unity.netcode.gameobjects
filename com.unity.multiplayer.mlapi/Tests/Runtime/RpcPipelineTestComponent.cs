using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;

namespace MLAPI.RuntimeTests
{
    public interface IMLAPIUnitTestObject
    {
        public bool IsTestComplete();
    }

    public class RpcPipelineTestComponent : NetworkedBehaviour, IMLAPIUnitTestObject
    {
        public bool PingSelfEnabled;

        //
        public int MaxIterations = 2;


        // Start is called before the first frame update
        void Start()
        {
            m_Serverparms.Send.UpdateStage = NetworkUpdateStage.Initialization;
            m_Clientparms.Send.UpdateStage = NetworkUpdateStage.Update;
        }

        public bool IsTestComplete()
        {
            if (m_Counter >= MaxIterations)
            {
                return true;
            }
            return false;
        }

        private int m_Counter = 0;
        private float m_NextUpdate = 0.0f;
        private ServerRpcParams m_Serverparms;
        private ClientRpcParams m_Clientparms;
        private NetworkUpdateStage m_LastUpdateStage;

        // Update is called once per frame
        void Update()
        {
            if (NetworkingManager.Singleton.IsListening && PingSelfEnabled && m_NextUpdate < Time.realtimeSinceStartup)
            {
                if (NetworkingManager.Singleton.IsListening && PingSelfEnabled && m_NextUpdate < Time.realtimeSinceStartup)
                {
                    //As long as testing isn't completed, keep testing
                    if(!IsTestComplete())
                    {
                        m_NextUpdate = Time.realtimeSinceStartup + 0.5f;
                        m_LastUpdateStage = m_Serverparms.Send.UpdateStage;
                        StagesSent.Add(m_LastUpdateStage);
                        PingMySelfServerRPC(m_Counter, m_Serverparms);
                        m_Clientparms.Send.UpdateStage = m_Serverparms.Send.UpdateStage;
                        switch (m_Serverparms.Send.UpdateStage)
                        {
                            case NetworkUpdateStage.Initialization:
                                {
                                    m_Serverparms.Send.UpdateStage = NetworkUpdateStage.EarlyUpdate;
                                    break;
                                }
                            case NetworkUpdateStage.EarlyUpdate:
                                {
                                    m_Serverparms.Send.UpdateStage = NetworkUpdateStage.FixedUpdate;
                                    break;
                                }
                            case NetworkUpdateStage.FixedUpdate:
                                {
                                    m_Serverparms.Send.UpdateStage = NetworkUpdateStage.PreUpdate;
                                    break;
                                }
                            case NetworkUpdateStage.PreUpdate:
                                {
                                    m_Serverparms.Send.UpdateStage = NetworkUpdateStage.Update;
                                    break;
                                }
                            case NetworkUpdateStage.Update:
                                {
                                    m_Serverparms.Send.UpdateStage = NetworkUpdateStage.PreLateUpdate;
                                    break;
                                }
                            case NetworkUpdateStage.PreLateUpdate:
                                {
                                    m_Serverparms.Send.UpdateStage = NetworkUpdateStage.PostLateUpdate;
                                    break;
                                }
                            case NetworkUpdateStage.PostLateUpdate:
                                {
                                    m_Serverparms.Send.UpdateStage = NetworkUpdateStage.Initialization;

                                    break;
                                }
                        }
                    }
                }
            }
        }


        List<NetworkUpdateStage> ServerStagesReceived = new List<NetworkUpdateStage>();
        List<NetworkUpdateStage> ClientStagesReceived = new List<NetworkUpdateStage>();
        List<NetworkUpdateStage> StagesSent = new List<NetworkUpdateStage>();

        /// <summary>
        /// ValidateUpdateStages
        /// Assures all update stages were in alginment with one another
        /// </summary>
        /// <returns>true or false</returns>
        public bool ValidateUpdateStages()
        {
            bool Validated = false;
            if ( ServerStagesReceived.Count == ClientStagesReceived.Count && ClientStagesReceived.Count == StagesSent.Count )
            {
                for(int i = 0; i < StagesSent.Count; i++)
                {
                    NetworkUpdateStage currentStage = StagesSent[i];
                    if(ServerStagesReceived[i] != currentStage)
                    {
                        Debug.LogFormat("ServerRpc Update Stage ( {0} ) is not equal to the current update stage ( {1} ) ", ServerStagesReceived[i].ToString(), currentStage.ToString());
                        return Validated;
                    }
                    if(ClientStagesReceived[i] != currentStage )
                    {
                        Debug.LogFormat("ClientRpc Update Stage ( {0} ) is not equal to the current update stage ( {1} ) ", ServerStagesReceived[i].ToString(), currentStage.ToString());
                        return Validated;
                    }
                }
                Validated = true;
            }
            return Validated;
        }

        /// <summary>
        /// PingMySelfServerRPC
        /// </summary>
        /// <param name="pingnumber">current number of pings</param>
        /// <param name="parameters">server rpc parameters</param>
        [ServerRpc]
        void PingMySelfServerRPC(int pingnumber, ServerRpcParams parameters)
        {
            Debug.Log("[HostClient][ServerRpc] invoked during the " + parameters.Receive.UpdateStage.ToString() + " stage.");
            m_Clientparms.Send.UpdateStage = parameters.Receive.UpdateStage;
            ServerStagesReceived.Add(m_Clientparms.Send.UpdateStage);
            PingMySelfClientRpc(m_Counter, m_Clientparms);

            //If we reached the last update state, then go ahead and increment our iteration counter
            if(parameters.Receive.UpdateStage == NetworkUpdateStage.PostLateUpdate)
            {
                m_Counter++;
            }

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
            ClientStagesReceived.Add(m_Clientparms.Send.UpdateStage);
            Debug.Log("[HostServer][ClientRpc] invoked during the " + parameters.Receive.UpdateStage.ToString() + " stage. (previous output line should confirm this)");
        }
    }
}
