using System.Collections.Generic;
using UnityEngine;
using MLAPI.Messaging;

namespace MLAPI.RuntimeTests
{
    /// <summary>
    /// Used in conjunction with the RpcQueueTest to validate:
    /// - Sending and Receiving pipeline to validate that both sending and receiving pipelines are functioning properly.
    /// - Usage of the ServerRpcParams.Send.UpdateStage and ClientRpcParams.Send.UpdateStage functionality.
    /// - Rpcs receive will be invoked at the appropriate NetworkUpdateStage.
    /// </summary>
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
        private void Start()
        {
            m_Serverparams.Send.UpdateStage = NetworkUpdateStage.Initialization;
            m_Clientparams.Send.UpdateStage = NetworkUpdateStage.Update;

            m_MaxStagesSent = (System.Enum.GetValues(typeof(NetworkUpdateStage)).Length)*MaxIterations;

            //Start out with this being true (for first sequence)
            m_ClientReceivedRpc = true;
        }

        /// <summary>
        /// Determine if we have iterated over more than our maximum stages we want to test
        /// </summary>
        /// <returns>true or false (did we exceed the max iterations or not?)</returns>
        public bool ExceededMaxIterations()
        {
            if(m_StagesSent.Count > m_MaxStagesSent && m_MaxStagesSent > 0)
            {
                return true;
            }
            return false;
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

        private bool m_ClientReceivedRpc;
        private int m_Counter = 0;
        private int m_MaxStagesSent = 0;
        private ServerRpcParams m_Serverparams;
        private ClientRpcParams m_Clientparams;
        private NetworkUpdateStage m_LastUpdateStage;

        // Update is called once per frame
        private void Update()
        {
            if (NetworkingManager.Singleton.IsListening && PingSelfEnabled && m_ClientReceivedRpc)
            {
                //Reset this for the next sequence of rpcs
                m_ClientReceivedRpc = false;

                //As long as testing isn't completed, keep testing
                if (!IsTestComplete() && m_StagesSent.Count < m_MaxStagesSent)
                {
                    m_LastUpdateStage = m_Serverparams.Send.UpdateStage;
                    m_StagesSent.Add(m_LastUpdateStage);
                    PingMySelfServerRPC(m_Serverparams);
                    switch (m_Serverparams.Send.UpdateStage)
                    {
                        case NetworkUpdateStage.Initialization:
                            {
                                m_Serverparams.Send.UpdateStage = NetworkUpdateStage.EarlyUpdate;
                                break;
                            }
                        case NetworkUpdateStage.EarlyUpdate:
                            {
                                m_Serverparams.Send.UpdateStage = NetworkUpdateStage.FixedUpdate;
                                break;
                            }
                        case NetworkUpdateStage.FixedUpdate:
                            {
                                m_Serverparams.Send.UpdateStage = NetworkUpdateStage.PreUpdate;
                                break;
                            }
                        case NetworkUpdateStage.PreUpdate:
                            {
                                m_Serverparams.Send.UpdateStage = NetworkUpdateStage.Update;
                                break;
                            }
                        case NetworkUpdateStage.Update:
                            {
                                m_Serverparams.Send.UpdateStage = NetworkUpdateStage.PreLateUpdate;
                                break;
                            }
                        case NetworkUpdateStage.PreLateUpdate:
                            {
                                m_Serverparams.Send.UpdateStage = NetworkUpdateStage.PostLateUpdate;
                                break;
                            }
                        case NetworkUpdateStage.PostLateUpdate:
                            {
                                m_Serverparams.Send.UpdateStage = NetworkUpdateStage.Initialization;

                                break;
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
            var validated = false;
            if (m_ServerStagesReceived.Count == m_ClientStagesReceived.Count && m_ClientStagesReceived.Count == m_StagesSent.Count)
            {
                for (int i = 0; i < m_StagesSent.Count; i++)
                {
                    NetworkUpdateStage currentStage = m_StagesSent[i];
                    if (m_ServerStagesReceived[i] != currentStage)
                    {
                        Debug.LogFormat("ServerRpc Update Stage ( {0} ) is not equal to the current update stage ( {1} ) ", m_ServerStagesReceived[i].ToString(), currentStage.ToString());
                        return validated;
                    }
                    if (m_ClientStagesReceived[i] != currentStage)
                    {
                        Debug.LogFormat("ClientRpc Update Stage ( {0} ) is not equal to the current update stage ( {1} ) ", m_ClientStagesReceived[i].ToString(), currentStage.ToString());
                        return validated;
                    }
                }
                validated = true;
            }
            return validated;
        }

        /// <summary>
        /// Server side RPC for testing
        /// </summary>
        /// <param name="parameters">server rpc parameters</param>
        [ServerRpc]
        private void PingMySelfServerRPC(ServerRpcParams parameters)
        {
            Debug.Log("[HostClient][ServerRpc] invoked during the " + parameters.Receive.UpdateStage.ToString() + " stage.");
            m_Clientparams.Send.UpdateStage = parameters.Receive.UpdateStage;
            m_ServerStagesReceived.Add(parameters.Receive.UpdateStage);
            PingMySelfClientRpc(m_Clientparams);
        }

        /// <summary>
        /// Client Side RPC called by PingMySelfServerRPC to validate both Client->Server and Server-Client pipeline is working
        /// </summary>
        /// <param name="parameters">client rpc parameters</param>
        [ClientRpc]
        private void PingMySelfClientRpc(ClientRpcParams parameters)
        {
            m_ClientStagesReceived.Add(parameters.Receive.UpdateStage);
            Debug.Log("[HostServer][ClientRpc] invoked during the " + parameters.Receive.UpdateStage.ToString() + " stage. (previous output line should confirm this)");

            //If we reached the last update state, then go ahead and increment our iteration counter
            if (parameters.Receive.UpdateStage == NetworkUpdateStage.PostLateUpdate)
            {
                m_Counter++;
            }
            m_ClientReceivedRpc = true;
        }
    }
}
