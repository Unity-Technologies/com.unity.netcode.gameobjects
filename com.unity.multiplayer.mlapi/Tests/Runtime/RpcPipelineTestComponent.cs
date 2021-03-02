using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;

namespace MLAPI.RuntimeTests
{
    public class RpcPipelineTestComponent : NetworkedBehaviour
    {
        /// <summary>
        /// Allows the external RPCQueueTest to begin testing or stop it
        /// </summary>
        public bool PingSelfEnabled;

        /// <summary>
        /// How many times will we iterate through the various NetworkUpdateStage values?
        /// (defaults to 2)
        /// </summary>
        public int MaxIterations = 2;


        // Start is called before the first frame update
        void Start()
        {
            m_Serverparms.Send.UpdateStage = NetworkUpdateStage.Initialization;
            m_Clientparms.Send.UpdateStage = NetworkUpdateStage.Update;
        }

        /// <summary>
        /// Returns back whether the test has completed the total number of iterations
        /// </summary>
        /// <returns></returns>
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
                    if (!IsTestComplete())
                    {
                        m_NextUpdate = Time.realtimeSinceStartup + 0.5f;
                        m_LastUpdateStage = m_Serverparms.Send.UpdateStage;
                        m_StagesSent.Add(m_LastUpdateStage);
                        PingMySelfServerRPC(m_Serverparms);
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


        private List<NetworkUpdateStage> m_ServerStagesReceived = new List<NetworkUpdateStage>();
        private List<NetworkUpdateStage> m_ClientStagesReceived = new List<NetworkUpdateStage>();
        private List<NetworkUpdateStage> m_StagesSent = new List<NetworkUpdateStage>();

        /// <summary>
        /// Assures all update stages were in alginment with one another
        /// </summary>
        /// <returns>true or false</returns>
        public bool ValidateUpdateStages()
        {
            var Validated = false;
            if (m_ServerStagesReceived.Count == m_ClientStagesReceived.Count && m_ClientStagesReceived.Count == m_StagesSent.Count)
            {
                for (int i = 0; i < m_StagesSent.Count; i++)
                {
                    NetworkUpdateStage currentStage = m_StagesSent[i];
                    if (m_ServerStagesReceived[i] != currentStage)
                    {
                        Debug.LogFormat("ServerRpc Update Stage ( {0} ) is not equal to the current update stage ( {1} ) ", m_ServerStagesReceived[i].ToString(), currentStage.ToString());
                        return Validated;
                    }
                    if (m_ClientStagesReceived[i] != currentStage)
                    {
                        Debug.LogFormat("ClientRpc Update Stage ( {0} ) is not equal to the current update stage ( {1} ) ", m_ClientStagesReceived[i].ToString(), currentStage.ToString());
                        return Validated;
                    }
                }
                Validated = true;
            }
            return Validated;
        }

        /// <summary>
        /// Server side RPC for testing
        /// </summary>
        /// <param name="parameters">server rpc parameters</param>
        [ServerRpc]
        void PingMySelfServerRPC(ServerRpcParams parameters)
        {
            Debug.Log("[HostClient][ServerRpc] invoked during the " + parameters.Receive.UpdateStage.ToString() + " stage.");
            m_Clientparms.Send.UpdateStage = parameters.Receive.UpdateStage;
            m_ServerStagesReceived.Add(m_Clientparms.Send.UpdateStage);
            PingMySelfClientRpc(m_Clientparms);
        }

        /// <summary>
        /// Client Side RPC called by PingMySelfServerRPC to validate both Client->Server and Server-Client pipeline is working
        /// </summary>
        /// <param name="parameters">client rpc parameters</param>
        [ClientRpc]
        void PingMySelfClientRpc(ClientRpcParams parameters)
        {
            m_ClientStagesReceived.Add(m_Clientparms.Send.UpdateStage);
            Debug.Log("[HostServer][ClientRpc] invoked during the " + parameters.Receive.UpdateStage.ToString() + " stage. (previous output line should confirm this)");

            //If we reached the last update state, then go ahead and increment our iteration counter
            if (parameters.Receive.UpdateStage == NetworkUpdateStage.PostLateUpdate)
            {
                m_Counter++;
            }
        }
    }
}
