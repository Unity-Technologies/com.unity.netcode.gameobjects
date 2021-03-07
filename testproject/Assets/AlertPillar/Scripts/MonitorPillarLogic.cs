using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.NetworkVariable;

namespace AlertPillar
{
    public class MonitorPillarLogic : NetworkBehaviour
    {
        public Material Unalerted;
        public Material Alerted;

        public NetworkVariableBool IsAlerted { get; } = new NetworkVariableBool();


        // Update is called once per frame
        void Update()
        {
            if (IsServer)
            {
                List<TestPlayerLogic> playerLogics = NetworkManager.FindObjectsOfTypeInScene<TestPlayerLogic>();
                if(playerLogics.Count > 0 )
                {
                    float distanceToPlayer = (playerLogics[0].transform.position - transform.position).magnitude;
                    IsAlerted.Value = distanceToPlayer < 3;
                }
            }
            if (IsClient)
            {
                GetComponent<Renderer>().material = IsAlerted.Value ? Alerted : Unalerted;
            }
        }
    }
}

